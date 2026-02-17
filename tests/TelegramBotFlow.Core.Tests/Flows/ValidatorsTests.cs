using FluentAssertions;
using TelegramBotFlow.Core.Flows;

namespace TelegramBotFlow.Core.Tests.Flows;

public sealed class ValidatorsTests
{
    [Theory]
    [InlineData("test@mail.com", true)]
    [InlineData("user@domain.org", true)]
    [InlineData("not-email", false)]
    [InlineData("@domain.com", false)]
    [InlineData("user@", false)]
    [InlineData("", false)]
    public void Email_ValidatesCorrectly(string input, bool isValid)
    {
        var result = Validators.Email(input);

        if (isValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData("-1", true)]
    [InlineData("0", true)]
    [InlineData("abc", false)]
    [InlineData("1.5", false)]
    [InlineData("", false)]
    public void Integer_ValidatesCorrectly(string input, bool isValid)
    {
        var result = Validators.Integer(input);

        if (isValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("+79991234567", true)]
    [InlineData("89991234567", true)]
    [InlineData("+1234567890", true)]
    [InlineData("123", false)]
    [InlineData("phone", false)]
    public void Phone_ValidatesCorrectly(string input, bool isValid)
    {
        var result = Validators.Phone(input);

        if (isValid)
            result.Should().BeNull();
        else
            result.Should().NotBeNull();
    }

    [Fact]
    public void NotEmpty_RejectsEmptyAndWhitespace()
    {
        Validators.NotEmpty("").Should().NotBeNull();
        Validators.NotEmpty("   ").Should().NotBeNull();
        Validators.NotEmpty("text").Should().BeNull();
    }

    [Fact]
    public void MaxLength_ValidatesCorrectly()
    {
        var validator = Validators.MaxLength(5);

        validator("hello").Should().BeNull();
        validator("hi").Should().BeNull();
        validator("toolong").Should().NotBeNull();
    }

    [Fact]
    public void Combine_RunsAllValidators()
    {
        var validator = Validators.Combine(Validators.NotEmpty, Validators.MaxLength(5));

        validator("").Should().NotBeNull();
        validator("toolong").Should().NotBeNull();
        validator("ok").Should().BeNull();
    }
}
