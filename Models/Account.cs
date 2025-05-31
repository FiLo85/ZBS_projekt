using System.ComponentModel.DataAnnotations;

namespace WeatherChecker_FO.Models
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string? Kod2FA { get; set; }

        public DateTime? Kod2FAWaznyDo { get; set; }

        public int NieudaneProbyLogowania { get; set; } = 0;
        public DateTime? BlokadaDo { get; set; }

    }
}
