using System;
using System.Collections.Generic;

namespace ASIB.Models;

public partial class Profile
{
    public long ProfileId { get; set; }

    public long? UserId { get; set; }

    public string? Bio { get; set; }

    public string? Skills { get; set; }

    public string? Experience { get; set; }

    public bool? PrivacySetting { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public string? Headline { get; set; }
}
