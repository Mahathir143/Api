namespace Api.Models.DTOs
{
    public class MenuReorderDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
        public int? ParentId { get; set; }
    }
}
