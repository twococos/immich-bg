namespace ImmichBg.Immich;

// Only the fields we use; System.Text.Json ignores the rest.

internal sealed record ServerVersionDto(int Major, int Minor, int Patch);

internal sealed record UserDto(string Id, string Name, string Email);

internal sealed record AlbumDto(string Id, string AlbumName, int AssetCount, DateTimeOffset UpdatedAt);

internal sealed record SearchResponseDto(SearchAssetsDto Assets);

internal sealed record SearchAssetsDto(List<AssetDto> Items, int Total, string? NextPage, string? NextCursor);

internal sealed record AssetDto(
    string Id,
    string Type,
    string Checksum,
    DateTimeOffset FileCreatedAt,
    DateTimeOffset? LocalDateTime,
    DateTimeOffset UpdatedAt,
    bool IsTrashed,
    bool IsEdited,
    int? Width,
    int? Height,
    ExifDto? ExifInfo);

internal sealed record ExifDto(int? ExifImageWidth, int? ExifImageHeight, string? Orientation);

public sealed record AlbumInfo(string Id, string Name, int AssetCount)
{
    public override string ToString() => $"{Name} ({AssetCount})";
}

/// <summary>An image on the server, reduced to what the sync needs.</summary>
/// <param name="Version">Changes when the image content changes (checksum, or edit time for edited assets).</param>
/// <param name="LocalTakenAt">Capture time in the photo's local time zone, used for file names.</param>
/// <param name="Width">Displayed width (orientation applied), 0 if unknown.</param>
public sealed record RemoteAsset(
    string Id,
    string Version,
    DateTimeOffset TakenAt,
    DateTime LocalTakenAt,
    int Width,
    int Height,
    bool IsEdited);
