namespace CloudStorages.Server.Dtos.Responses
{
    public sealed record ContainerResponse
    {
        public string Name { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
