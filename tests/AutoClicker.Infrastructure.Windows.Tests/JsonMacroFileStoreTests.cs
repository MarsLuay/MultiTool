using AutoClicker.Core.Enums;
using AutoClicker.Core.Models;
using AutoClicker.Infrastructure.Windows.Persistence;
using FluentAssertions;

namespace AutoClicker.Infrastructure.Windows.Tests;

public sealed class JsonMacroFileStoreTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "AutoClicker.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_AndLoadAsync_ShouldRoundTripMacro()
    {
        Directory.CreateDirectory(workingDirectory);
        var filePath = Path.Combine(workingDirectory, "sample.acmacro.json");
        var store = new JsonMacroFileStore();

        var expected = new RecordedMacro(
            "Farm Loop",
            [
                new MacroEvent(TimeSpan.FromMilliseconds(0), MacroEventKind.MouseMove, Position: new ScreenPoint(100, 200)),
                new MacroEvent(TimeSpan.FromMilliseconds(25), MacroEventKind.MouseButtonDown, MouseButton: ClickMouseButton.Left, Position: new ScreenPoint(100, 200)),
                new MacroEvent(TimeSpan.FromMilliseconds(80), MacroEventKind.MouseButtonUp, MouseButton: ClickMouseButton.Left, Position: new ScreenPoint(100, 200)),
                new MacroEvent(TimeSpan.FromMilliseconds(120), MacroEventKind.KeyDown, VirtualKey: 0x51),
                new MacroEvent(TimeSpan.FromMilliseconds(150), MacroEventKind.KeyUp, VirtualKey: 0x51),
            ],
            TimeSpan.FromMilliseconds(150),
            DateTimeOffset.Parse("2026-03-13T12:00:00+00:00"));

        await store.SaveAsync(filePath, expected);
        var actual = await store.LoadAsync(filePath);

        actual.Name.Should().Be(expected.Name);
        actual.Duration.Should().Be(expected.Duration);
        actual.RecordedAt.Should().Be(expected.RecordedAt);
        actual.Events.Should().Equal(expected.Events);
    }

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, true);
        }
    }
}
