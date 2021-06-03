using System;
using System.Collections.Generic;
using Dex.CreditCardType.Resolver;
using NUnit.Framework;

namespace Dex.CreditCard.Tests
{
    public class Tests
    {
        // http://support.worldpay.com/support/kb/bg/testandgolive/tgl5103.html

        private readonly IDictionary<string, CardType> _validCards = new Dictionary<string, CardType>
        {
            {"2201382000000013", CardType.Mir},
            {"2200000000000046", CardType.Mir},
            {"4169773331987017", CardType.Visa},
            {"4658958254583145", CardType.Visa},
            {"4911830000000", CardType.Visa},
            {"4462030000000000", CardType.Visa},
            {"5410710000901089", CardType.MasterCard},
            {"5582128534772839", CardType.MasterCard},
            {"349101032764066", CardType.AmericanExpress},
            {"371305972529535", CardType.AmericanExpress},
            {"6011683204539909", CardType.Discover},
            {"6011000400000000", CardType.Discover},
            {"3569239206830557", CardType.JCB},
            {"3589295535870728", CardType.JCB},
            {"6759649826438453", CardType.Maestro},
            {"6799990100000000019", CardType.Maestro},
            {"639002389084992585", CardType.Maestro},
            {"36700102000000", CardType.DinersClub},
            {"36148900647913", CardType.DinersClub},
        };

        private string _invalidCard = "2201382000000010";


        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ResolveCardTypeTest1()
        {
            foreach (var card in _validCards)
            {
                Assert.AreEqual(card.Value, CreditCardTypeDetector.FindType(card.Key));
            }
        }

        [Test]
        public void LyhnCardTypeTest1()
        {
            foreach (var card in _validCards)
            {
                Assert.True(LuhnAlgorithm.HasValidCheckDigit(card.Key));
            }
        }

        [Test]
        public void LyhnCardTypeInvalidTest1()
        {
            Assert.False(LuhnAlgorithm.HasValidCheckDigit(_invalidCard));
        }

        [Test]
        [TestCase("ss")]
        [TestCase("512512 6124652 36551243512451242341234421341234")]
        [TestCase("512512 6124652 3655124351232152")]
        [TestCase("51251261 246523634263214123411")]
        public void LyhnInvalidStringTest2(string arg)
        {
            Assert.Catch<ArgumentException>(() =>
            {
                Assert.False(LuhnAlgorithm.HasValidCheckDigit(arg));
            });
        }     
    }
}