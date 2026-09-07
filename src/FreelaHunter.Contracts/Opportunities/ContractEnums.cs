namespace FreelaHunter.Contracts.Opportunities;

public enum ContractMarketplace
{
    Unknown = 0,
    Workana = 1,
    Upwork = 2,
    Freelancer = 3,
    Nerdin = 4,
    Other = 99
}

public enum ContractIntakeChannel
{
    Manual = 0,
    Email = 1
}

public enum ContractOpportunityStatus
{
    New = 0,
    Shortlisted = 1,
    Ignored = 2,
    Archived = 99
}

public enum ContractBudgetKind
{
    Unknown = 0,
    Negotiable = 1,
    Exact = 2,
    Range = 3,
    UpTo = 4,
    From = 5
}

public enum ContractBudgetBasis
{
    Unknown = 0,
    FixedProject = 1,
    Hourly = 2,
    Monthly = 3
}
