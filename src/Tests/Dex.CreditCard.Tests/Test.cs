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
            {"36700102000000", CardType.DinersClub},
            {"36148900647913", CardType.DinersClub},
        };

        private string _invalidCard = "2201382000000010";
        private string _invalidCardLen = "1234";
        private string _invalidCardFormat = "abcd12345abcd";
        private string _invalidCardFormatSpace = "3614 8900 647 913";
        private string _invalidCardFormatDash = "3614-8900-647-913";

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
        public void LyhnCardTypeFormatCheckLen()
        {
            Assert.Throws(typeof(ArgumentException), () => LuhnAlgorithm.HasValidCheckDigit(_invalidCardLen));
        }

        [Test]
        public void LyhnCardTypeFormatCheckInvalid()
        {
            Assert.Throws(typeof(ArgumentException), () => LuhnAlgorithm.HasValidCheckDigit(_invalidCardFormat));
        }

        [Test]
        public void LyhnCardTypeFormatCheckInvalidSpace()
        {
            Assert.Throws(typeof(ArgumentException), () => LuhnAlgorithm.HasValidCheckDigit(_invalidCardFormatSpace));
        }

        [Test]
        public void LyhnCardTypeFormatCheckInvalidDash()
        {
            Assert.Throws(typeof(ArgumentException), () => LuhnAlgorithm.HasValidCheckDigit(_invalidCardFormatDash));
        }

        [Test]
        public void ResolveCardTypeFormatCheckLen()
        {
            Assert.Throws(typeof(ArgumentException), () => CreditCardTypeDetector.FindType(_invalidCardLen));
        }

        [Test]
        public void ResolveCardTypeFormatCheckInvalid()
        {
            Assert.Throws(typeof(ArgumentException), () => CreditCardTypeDetector.FindType(_invalidCardFormat));
        }

        [Test]
        public void ResolveCardTypeFormatCheckInvalidSpace()
        {
            Assert.Throws(typeof(ArgumentException), () => CreditCardTypeDetector.FindType(_invalidCardFormatSpace));
        }

        [Test]
        public void ResolveCardTypeFormatCheckInvalidDash()
        {
            Assert.Throws(typeof(ArgumentException), () => CreditCardTypeDetector.FindType(_invalidCardFormatDash));
        }
    }
}