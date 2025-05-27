namespace WellUp.Core.Database
{
    public class Admin
    {
        public int AdminId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AdminName { get; set; }
        public DateTime? DateModified { get; set; }
    }
}