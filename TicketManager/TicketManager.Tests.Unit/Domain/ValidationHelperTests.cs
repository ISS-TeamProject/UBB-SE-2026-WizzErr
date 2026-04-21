using FluentAssertions;
using TicketManager.Domain;

namespace TicketManager.Tests.Unit.Domain;

public class ValidationHelperTests
{
    [Fact]
    public void TestThatValidEmailIsAccepted()
    {
        var result = ValidationHelper.IsValidEmail("ion.popescu@gmail.com");
        result.Should().BeTrue();
    }

    [Fact]
    public void TestThatEmailWithoutAtIsRejected()
    {
        var result = ValidationHelper.IsValidEmail("ion.popescu.com");
        result.Should().BeFalse();
    }

    [Fact]
    public void TestThatEmptyEmailIsRejected()
    {
        var result = ValidationHelper.IsValidEmail("");
        result.Should().BeFalse();
    }

    [Fact]
    public void TestThatNullEmailIsRejected()
    {
        var result = ValidationHelper.IsValidEmail(null);
        result.Should().BeFalse();
    }

    [Fact]
    public void TestThatValidRomanianPhoneIsAccepted()
    {
        var result = ValidationHelper.IsValidPhone("0722112233");
        result.Should().BeTrue();
    }

    [Fact]
    public void TestThatPhoneWithHyphensIsRejected()
    {
        var result = ValidationHelper.IsValidPhone("0722-112-233");
        result.Should().BeFalse();
    }

    [Fact]
    public void TestThatPhoneWithLettersIsRejected()
    {
        var result = ValidationHelper.IsValidPhone("0722abc123");
        result.Should().BeFalse();
    }

    [Fact]
    public void TestThatTooShortPhoneIsRejected()
    {
        var result = ValidationHelper.IsValidPhone("12345");
        result.Should().BeFalse();
    }
}
