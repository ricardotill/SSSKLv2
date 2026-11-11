using System.Text.Json;
using System.Text.Json.Serialization;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core;

/// <summary>
/// Source-generated (reflection-free) JSON metadata for everything the app serializes, so the app can
/// be published with Native AOT and full trimming. Add a type here before (de)serializing it.
/// Web defaults: camelCase names, case-insensitive reads, numbers from strings.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(AccessTokenResponse))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(CurrentUser))]
[JsonSerializable(typeof(OrderInitialize))]
[JsonSerializable(typeof(OrderSubmit))]
[JsonSerializable(typeof(UserStats))]
[JsonSerializable(typeof(List<AchievementEntry>))]
[JsonSerializable(typeof(GlobalSetting))]
[JsonSerializable(typeof(Base64FileUpload))]
[JsonSerializable(typeof(SelfUpdate))]
[JsonSerializable(typeof(TwoFactorRequest))]
[JsonSerializable(typeof(TwoFactorStatus))]
[JsonSerializable(typeof(List<NotificationItem>))]
[JsonSerializable(typeof(Page<OrderItem>))]
[JsonSerializable(typeof(Page<TopUpItem>))]
[JsonSerializable(typeof(Page<UserSummary>))]
[JsonSerializable(typeof(UserSummary))]
[JsonSerializable(typeof(List<Product>))]
[JsonSerializable(typeof(List<LeaderboardEntry>))]
[JsonSerializable(typeof(List<AchievementListing>))]
[JsonSerializable(typeof(Page<EventItem>))]
[JsonSerializable(typeof(EventItem))]
[JsonSerializable(typeof(EventRsvp))]
[JsonSerializable(typeof(Page<QuoteItem>))]
[JsonSerializable(typeof(QuoteItem))]
[JsonSerializable(typeof(List<Reaction>))]
[JsonSerializable(typeof(ToggleReaction))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(TokenSet))]
[JsonSerializable(typeof(string))]
public sealed partial class ApiJsonContext : JsonSerializerContext;
