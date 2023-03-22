using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AssetManagementApi.Models;

public class ScheduledBackgroundJob
{

    [Key]
    public int Id { get; set; }

    public string JobId { get; set; } = null!;

    public Guid Token { get; set; }
}