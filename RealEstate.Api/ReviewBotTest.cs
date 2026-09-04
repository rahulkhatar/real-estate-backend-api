namespace RealEstate.Api;

// Throwaway file to verify the Claude PR Review workflow actually catches a real bug and
// submits a blocking review verdict. Not wired into DI or routing. Will be deleted, this PR
// will not be merged.
public static class ReviewBotTest
{
    // Bug: inverted condition -- this returns true for a NEGATIVE or zero discount, and false
    // for any valid positive discount, which is backwards from what the name promises.
    public static bool IsValidDiscountPercentage(decimal discountPercentage)
    {
        return discountPercentage <= 0 || discountPercentage > 100;
    }
}
