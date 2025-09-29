using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Filters;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace Rumble.Platform.Common.Services;
public class ApiService : PlatformService
{
    public static ApiService Instance { get; private set; }
    private HttpClient HttpClient { get; init; }
    private CredentialCache CredentialCache { get; init; }
    internal RumbleJson DefaultHeaders { get; init; }

    private Dictionary<long, long> StatusCodes { get; init; }

    // TODO: Add origin (calling class), and do not honor requests coming from self
    public ApiService()
    {
        Instance = this;
        CredentialCache = new CredentialCache();
        HttpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            Credentials = CredentialCache
        });

        AssemblyName exe = Assembly.GetExecutingAssembly().GetName();
        DefaultHeaders = new RumbleJson
        {
            { "User-Agent", $"{exe.Name}/{exe.Version}" },
            { "Accept", "*/*" },
            { "Accept-Encoding", "gzip, deflate, br" }
        };
        StatusCodes = new Dictionary<long, long>();
    }

    internal void AddDigestAuth(string url, string username, string password)
    {
        const string TYPE = "Digest";
        
        Uri uri = new Uri(url);
        if (CredentialCache.GetCredential(uri, TYPE) != null)
            CredentialCache.Remove(uri, TYPE);
        CredentialCache.Add(uri, authType: TYPE, cred: new NetworkCredential(username, password));
    }

    internal async Task<HttpResponseMessage> MultipartFormPost(string url, MultipartFormDataContent content) => await HttpClient.PostAsync(url, content);

    /// <summary>
    /// Initializes the base HTTP request.
    /// </summary>
    /// <param name="url">The URL to hit.  If a partial URL is used, it is appended to the base of PlatformEnvironment.Url().</param>
    /// <param name="retries">How many times to retry the request if it fails.</param>
    public ApiRequest Request(string url, int retries = ApiRequest.DEFAULT_RETRIES) => Request(url, retries, prependEnvironment: true);

    internal ApiRequest Request(string url, int retries, bool prependEnvironment) => new(this, url, retries, prependEnvironment);

    internal RumbleJson Send(HttpRequestMessage message) => null;

    internal ApiResponse Send(ApiRequest request)
    {
        Task<ApiResponse> task = SendAsync(request);
        task.Wait();
        return task.Result;
    }
    
    internal async Task<ApiResponse> SendAsync(ApiRequest request)
    {
        HttpResponseMessage response = null;
        int code = -1;
        try
        {
            do
            {
                if (code != -1)
                {
                    Log.Local(Owner.Will, $"Request failed; retrying in {request.ExponentialBackoffMs}ms", data: new
                    {
                        BackoffMS = request.ExponentialBackoffMs,
                        RetriesRemaining = request.Retries,
                        Url = request.Url
                    });
                    Thread.Sleep(request.ExponentialBackoffMs);
                }

                response = await HttpClient.SendAsync(request);
                code = (int)response.StatusCode;
            } while ((code.Between(500, 599) || code.Between(300, 399)) && request.Retries-- > 0);
        }
        catch (Exception e)
        {
            // Don't infinitely log if failing to send to loggly
            if (!request.Url.Contains("loggly.com"))
                Log.Error(Owner.Default, $"Could not send request to '{request.Url}'.", data: new
                {
                    Request = request,
                    Response = response
                }, exception: e);
        }

        ApiResponse output = new(response, request);
        request.Complete(output);
        Record(output.StatusCode);
        return output;
    }

    private void Record(int code)
    {
        if (!StatusCodes.ContainsKey(code))
            StatusCodes[code] = 1;
        else
            StatusCodes[code]++;
    }

    private float SuccessPercentage
    {
        get
        {
            float total = StatusCodes.Sum(pair => pair.Value);
            float success = StatusCodes
                .Where(pair => pair.Key >= 200 && pair.Key < 300)
                .Sum(pair => pair.Value);
            return 100f * success / total;
        }
    }

    public override RumbleJson HealthStatus => new()
    {
        { Name, new RumbleJson
            {
                { "health", $"{SuccessPercentage} %" },
                { "responses", StatusCodes.OrderBy(pair => pair.Value) }
            }
        }
    };

    /// <summary>
    /// Tells token-service to ban the player in question.  This prevents tokens from being generated for that account.
    /// Your service's admin token must have permissions to interact with token-service to do this.
    /// </summary>
    /// <param name="accountId">The account in question to ban.</param>
    /// <param name="duration">The duration to ban the account for, in seconds.  If unspecified, the ban is permanent.</param>
    /// <param name="audiences">Used for selective bans; currently just a placeholder, not supported yet.  Once supported,
    /// a selective ban will still allow a player to generate tokens; those tokens just won't have permissions to use certain
    /// services (e.g. Leaderboards).
    /// </param>
    /// <param name="reason">A value for internal use to communicate why the ban was issued.</param>
    public void BanPlayer(string accountId, long? duration = null, Audience audiences = Audience.All, string reason = null)
    {
        RumbleJson payload = new()
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, accountId },
            {
                "ban", new Ban
                {
                    Expiration = Timestamp.Now + duration,
                    Reason = reason,
                    PermissionSet = (int)audiences
                }
            },
        };
        
        Request("/token/admin/ban")
            .AddAuthorization(DynamicConfig.Instance?.AdminToken)
            .SetPayload(payload)
            .OnSuccess(_ =>
            {
                Log.Info(Owner.Default, $"An account has been banned.", data: new
                {
                    AccountId = accountId,
                    Reason = reason
                });
                
                Get(out CacheService cache);
                cache.ClearToken(accountId);
            })
            .OnFailure(response =>
            {
                object data = new
                {
                    AccountId = accountId,
                    Help = "This could be an admin token permissions issue or other failure.",
                    Payload = payload,
                    Response = response.AsRumbleJson
                };
                if (PlatformEnvironment.IsProd)
                    Log.Critical(Owner.Will, "Unable to ban player.", data);
                else
                    Log.Error(Owner.Default, "Unable to ban player.", data);
            })
            .Post();
    }

    /// <summary>
    /// Tells token-service to unban the player in question.  This restores token generation for that account.
    /// Your service's admin token must have permissions to interact with token-service to do this.
    /// </summary>
    /// <param name="accountId">The account in question to ban.</param>
    /// <param name="audiences">Used for selective bans; currently just a placeholder, not supported yet.  Once supported,
    /// a selective ban will still allow a player to generate tokens; those tokens just won't have permissions to use certain
    /// services (e.g. Leaderboards).
    /// </param>
    public void UnbanPlayer(string accountId, Audience audiences = Audience.All) =>
        Request("/token/admin/unban")
            .AddAuthorization(DynamicConfig.Instance?.AdminToken)
            .SetPayload(new RumbleJson
            {
                { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, accountId },
                { TokenInfo.FRIENDLY_KEY_AUDIENCE, audiences }, // placeholder until token-service supports it
            })
            .OnSuccess(response => Log.Info(Owner.Default, $"An account has been unbanned.", data: new
            {
                AccountId = accountId
            }))
            .OnFailure(response =>
            {
                object data = new
                {
                    AccountId = accountId,
                    Help = "This could be an admin token permissions issue or other failure.",
                    Response = response.AsRumbleJson
                };
                if (PlatformEnvironment.IsProd)
                    Log.Critical(Owner.Will, "Unable to unban player.", data);
                else
                    Log.Error(Owner.Default, "Unable to unban player.", data);
            })
            .Patch();

    public string GenerateToken(string accountId, string screenname, string email, int discriminator, Audience audiences = Audience.All)
        => GenerateToken(accountId, screenname, email, discriminator, audiences, out _);
    public string GenerateToken(string accountId, string screenname, string email, int discriminator, Audience audiences, out TokenInfo token)
    {
        if (accountId == null || screenname == null || discriminator < 0)
            throw new TokenGenerationException("Insufficient user information for token generation.");

        Get(out DynamicConfig dc2);
        if (dc2 == null)
            throw new TokenGenerationException("Dynamic config is null; token generation is impossible.");
        
        string adminToken = dc2.AdminToken;
        if (adminToken == null)
            throw new TokenGenerationException("No admin token present in dynamic config.");

        GeoIPData geoData = null;
        try
        {
            geoData = GeoIPData.FromAddress((string)new HttpContextAccessor().HttpContext?.Items[PlatformResourceFilter.KEY_IP_ADDRESS]);
        }
        catch (Exception e)
        {
            Log.Warn(Owner.Default, $"Unable to load GeoIPData for token generation", exception: e);
        }

        string[] audience = audiences == Audience.All
            ? new string[] { audiences.GetDisplayName() }
            : audiences
                .GetFlags()
                .Select(en => en.GetDisplayName())
                .ToArray();
        
#if LOCAL
        string url = PlatformEnvironment.Url("http://localhost:5031/secured/token/generate");
#else
        string url = PlatformEnvironment.Url("/secured/token/generate");
#endif
        
        RumbleJson payload = new()
        {
            { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, accountId },
            { TokenInfo.FRIENDLY_KEY_SCREENNAME, screenname },
            { TokenInfo.FRIENDLY_KEY_REQUESTER, PlatformEnvironment.ServiceName },
            { TokenInfo.FRIENDLY_KEY_EMAIL_ADDRESS, email },
            { TokenInfo.FRIENDLY_KEY_DISCRIMINATOR, discriminator },
            { TokenInfo.FRIENDLY_KEY_IP_ADDRESS, geoData?.IPAddress },
            { TokenInfo.FRIENDLY_KEY_COUNTRY_CODE, geoData?.CountryCode },
            { TokenInfo.FRIENDLY_KEY_PERMISSION_SET, (int)audiences },
            { "days", PlatformEnvironment.IsLocal ? 3650 : 5 }
        };
        int code;
        
        const string KEY_LAST_TOKEN_GENERATED = "lastTokenGeneratedOn";
        const int FAILURE_THRESHOLD_SECONDS = 300; 

        Request(url)
            .AddAuthorization(adminToken)
            .SetRetries(5)
            .SetPayload(payload)
            .OnSuccess(_ => ConfigService.Instance?.Set(KEY_LAST_TOKEN_GENERATED, Timestamp.Now))
            .OnFailure(response =>
            {
                Log.Error(Owner.Will, "Unable to generate token.", data: new
                {
                    Payload = payload,
                    Response = response,
                    Url = response.RequestUrl,
                    Code = response.StatusCode
                });
            })
            .Post(out RumbleJson json, out code);

        try
        {
            token = json.Require<TokenInfo>("tokenInfo");
            // PLATF-6462: Second part to fixing the bans; when a service is requesting a new token be generated and includes
            // itself in the token audiences, we need to reject the token's distribution.  If the requested audience does NOT
            // include the current service, we can assume it's generating a token for other use cases, and may in fact still
            // be valid - for example, DMZ creating admin tokens.
            if (PlatformEnvironment.ProjectAudience != default && audiences.HasFlag(PlatformEnvironment.ProjectAudience))
            {
                if (!token.IsValidFor(PlatformEnvironment.ProjectAudience, out long? expiration))
                    throw new TokenBannedException("Token has been banned from the requesting resource.", expiration);
            }

            return json.Require<RumbleJson>("authorization").Require<string>("token");
        }
        catch (KeyNotFoundException)
        {
            throw new TokenGenerationException(json?.Optional<string>("message"));
        }
        catch (NullReferenceException)
        {
            throw new TokenGenerationException("Response was null.");
        }
        catch (TokenBannedException)
        {
            throw;
        }
    }

    public TokenInfo ValidateToken(string encryptedToken, string endpoint) => ValidateToken(encryptedToken, endpoint, context: null).Token;

    /// <summary>
    /// Validates .
    /// </summary>
    /// <param name="encryptedToken">The token from the Authorization Header.  It may either include or omit "Bearer ".</param>
    /// <param name="endpoint">The endpoint asking for authorization.  This is very helpful in diagnosing auth issues in Loggly.</param>
    /// <param name="context">The HTTP context</param>
    /// <returns>A TokenValidationResult, containing the token, any errors in the validation, and a success flag.</returns>
    public TokenValidationResult ValidateToken(string encryptedToken, string endpoint, HttpContext context)
    {
        encryptedToken = encryptedToken?.Replace("Bearer ", "");
        
        if (string.IsNullOrWhiteSpace(encryptedToken))
            return new TokenValidationResult
            {
                Error = "Token is empty or null.",
                Success = false,
                Token = null
            };
        
        TokenInfo output = null;
        string message = null;
        Get(out CacheService cache);

        if (!(cache?.HasValue(encryptedToken, out output) ?? false))
#if LOCAL
            Request(PlatformEnvironment.Url($"http://localhost:5031/token/validate?origin={PlatformEnvironment.ServiceName}&endpoint={endpoint}"))
#else
            Request(PlatformEnvironment.Url($"/token/validate?origin={PlatformEnvironment.ServiceName}&endpoint={endpoint}"))
#endif
                .AddAuthorization(encryptedToken)
                .SetRetries(5)
                .OnFailure(response =>
                {
                    message = response?.AsRumbleJson?.Optional<string>("message");
                })
                .OnSuccess(response =>
                {
                    RumbleJson json = response.AsRumbleJson;
                    message = json.Optional<string>("message");
                    output = json.Optional<TokenInfo>(TokenInfo.KEY_TOKEN_OUTPUT) ?? json.Require<TokenInfo>(TokenInfo.KEY_TOKEN_LEGACY_OUTPUT);
                    
                    output.Authorization = encryptedToken;
                    
                    // Store only valid tokens
                    cache?.Store(encryptedToken, output, expirationMS: TokenInfo.CACHE_EXPIRATION);
                })
                .Get();
        
        context ??= new HttpContextAccessor().HttpContext;
        if (context != null)
            context.Items[PlatformAuthorizationFilter.KEY_TOKEN] = output;

        return new TokenValidationResult
        {
            Error = message ?? "No message provided",
            Success = output != null,
            Token = output
        };
    }

    public bool InvalidateToken(TokenInfo token)
    {
        Request("token/admin/invalidate")
            .AddAuthorization(DynamicConfig.Instance.AdminToken)
            .SetPayload(new RumbleJson
            {
                { TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, token.AccountId }
            })
            .OnSuccess(_ =>
            {
                Log.Local(Owner.Default, $"Token invalidated for {token.AccountId}.");
                Get(out CacheService cache);
                cache?.ClearToken(token.AccountId);
            })
            .OnFailure(response => Log.Warn(Owner.Default, "Unable to invalidate token", data: new
            {
                Token = token
            }))
            .Patch();
        return true;
    }
}