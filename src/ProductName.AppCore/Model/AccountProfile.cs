namespace ProductName.AppCore.Model
{
    public sealed record AccountProfile
    {
        public string OperatorId { get; init; }
        public string DisplayName { get; init; }
        public string CompanyName { get; init; }
        public IReadOnlyList<BranchProfile> Branches { get; init; }

        public AccountProfile(string operatorId, string displayName, string companyName, IReadOnlyList<BranchProfile> branches)
        {
            OperatorId = operatorId;
            DisplayName = displayName;
            CompanyName = companyName;
            Branches = branches;
        }
    }
}