using MarketBlog;
using NUnit.Framework;

namespace MarketBlog.Tests;

[TestFixture]
public class PostInputValidatorTests
{
    [Test]
    public void Validate_WhenAllFieldsAreValid_ReturnsNoErrors()
    {
        // Arrange
        const string title = "A thoughtful title";
        const string content = "A story worth sharing.";
        const string author = "Alex Writer";
        const string category = "Ideas";
        const string excerpt = "A short preview.";

        // Act
        var errors = PostInputValidator.Validate(title, content, author, category, excerpt);

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WhenTitleIsMissing_ReturnsTitleError(string? title)
    {
        // Arrange
        const string content = "Story text";
        const string author = "Alex";

        // Act
        var errors = PostInputValidator.Validate(title, content, author, null, null);

        // Assert
        Assert.That(errors["title"], Is.EqualTo("Title is required."));
    }

    [Test]
    public void Validate_WhenTitleIsTooLong_ReturnsTitleLengthError()
    {
        // Arrange
        var title = new string('T', 121);

        // Act
        var errors = PostInputValidator.Validate(title, "Story text", "Alex", null, null);

        // Assert
        Assert.That(errors["title"], Is.EqualTo("Title must be 120 characters or fewer."));
    }

    [Test]
    public void Validate_WhenStoryIsMissing_ReturnsStoryError()
    {
        // Arrange
        const string title = "A title";
        const string author = "Alex";

        // Act
        var errors = PostInputValidator.Validate(title, "  ", author, null, null);

        // Assert
        Assert.That(errors["content"], Is.EqualTo("Story is required."));
    }

    [Test]
    public void Validate_WhenStoryIsTooLong_ReturnsStoryLengthError()
    {
        // Arrange
        var content = new string('S', 50001);

        // Act
        var errors = PostInputValidator.Validate("A title", content, "Alex", null, null);

        // Assert
        Assert.That(errors["content"], Is.EqualTo("Story must be 50000 characters or fewer."));
    }

    [Test]
    public void Validate_WhenAuthorIsMissing_ReturnsAuthorError()
    {
        // Arrange
        const string title = "A title";
        const string content = "Story text";

        // Act
        var errors = PostInputValidator.Validate(title, content, "", null, null);

        // Assert
        Assert.That(errors["author"], Is.EqualTo("Author name is required."));
    }

    [Test]
    public void Validate_WhenAuthorIsTooLong_ReturnsAuthorLengthError()
    {
        // Arrange
        var author = new string('A', 61);

        // Act
        var errors = PostInputValidator.Validate("A title", "Story text", author, null, null);

        // Assert
        Assert.That(errors["author"], Is.EqualTo("Author name must be 60 characters or fewer."));
    }

    [Test]
    public void Validate_WhenOptionalTopicAndIntroAreTooLong_ReturnsErrorsForBothFields()
    {
        // Arrange
        var category = new string('C', 41);
        var excerpt = new string('E', 221);

        // Act
        var errors = PostInputValidator.Validate("A title", "Story text", "Alex", category, excerpt);

        // Assert
        Assert.That(errors["category"], Is.EqualTo("Topic must be 40 characters or fewer."));
        Assert.That(errors["excerpt"], Is.EqualTo("Short intro must be 220 characters or fewer."));
    }

    [Test]
    public void Validate_WhenOptionalFieldsAreBlank_AllowsThem()
    {
        // Arrange
        const string title = "A title";
        const string content = "Story text";
        const string author = "Alex";

        // Act
        var errors = PostInputValidator.Validate(title, content, author, " ", "");

        // Assert
        Assert.That(errors, Is.Empty);
    }
}
