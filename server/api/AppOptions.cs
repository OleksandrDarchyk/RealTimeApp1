using System.ComponentModel.DataAnnotations;

namespace api;

public class AppOptions
{
    [Required, MinLength(1)]
    public string RedisConnectionString { get; set; } = "";

    [Required, MinLength(1)]
    public string DbConnectionString { get; set; } = "";

    [Required, MinLength(32)]
    public string Secret { get; set; } = "";
}
