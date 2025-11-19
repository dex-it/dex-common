using System.Diagnostics.CodeAnalysis;

namespace Dex.CreditCardType.Resolver
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CardType
    {
        Uncertain,
        Mir,
        Visa,
        MasterCard,
        AmericanExpress,
        Discover,
        JCB,
        DinersClub,
        Maestro,
        UnionPay
    }
}