using FluentAssertions;

using FluentResults;

using TypesShared.Results;

using UI.MessageService;

using Xunit;

namespace Tests.UI;

[Trait("Component", "UI")]
[Trait("Area", "MessagePanel")]
[Trait("Category", "Unit")]
public sealed class MessagePanelViewModelTests
{
	[Fact]
	public void AddError_IncreasesErrorCountByOne()
	{
		var panel = new MessagePanelViewModel();

		panel.AddError("msg", "src");

		panel.ErrorCount.Should().Be(1);
	}

	[Fact]
	public void AddError_AddsEntryWithErrorSeverity()
	{
		var panel = new MessagePanelViewModel();

		panel.AddError("test error", "src");

		panel.Entries.Should().ContainSingle(e => e.IsError);
	}

	[Fact]
	public void AddInfo_DoesNotIncrementErrorOrWarningCount()
	{
		var panel = new MessagePanelViewModel();

		panel.AddInfo("msg", "src");

		panel.ErrorCount.Should().Be(0);
		panel.WarningCount.Should().Be(0);
	}

	[Fact]
	public void AddError_SetsHasErrorsTrue()
	{
		var panel = new MessagePanelViewModel();

		panel.AddError("msg", "src");

		panel.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void HasErrors_IsFalse_WhenNoErrors()
	{
		var panel = new MessagePanelViewModel();

		panel.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void HasWarnings_IsFalse_WhenNoWarnings()
	{
		var panel = new MessagePanelViewModel();

		panel.HasWarnings.Should().BeFalse();
	}

	[Fact]
	public void ErrorCountText_Singular_WhenOneError()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("msg", "src");

		panel.ErrorCountText.Should().Be("1 Error");
	}

	[Fact]
	public void ErrorCountText_Plural_WhenTwoErrors()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("msg1", "src");
		panel.AddError("msg2", "src");

		panel.ErrorCountText.Should().Be("2 Errors");
	}

	[Fact]
	public void WarningCountText_Singular_WhenOneWarning()
	{
		var panel = new MessagePanelViewModel();
		panel.RefreshReasons(new List<IReason> { new Warning("msg") });

		panel.WarningCountText.Should().Be("1 Warning");
	}

	[Fact]
	public void StatusErrorSummary_BothErrorsAndWarnings()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e", "src");
		panel.RefreshReasons(new List<IReason> { new Warning("w") });

		panel.StatusErrorSummary.Should().Be("1 Error, 1 Warning");
	}

	[Fact]
	public void StatusErrorSummary_OnlyErrors()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e1", "src");
		panel.AddError("e2", "src");

		panel.StatusErrorSummary.Should().Be("2 Errors");
	}

	[Fact]
	public void StatusErrorSummary_OnlyWarnings()
	{
		var panel = new MessagePanelViewModel();
		panel.RefreshReasons(new List<IReason> { new Warning("w") });

		panel.StatusErrorSummary.Should().Be("1 Warning");
	}

	[Fact]
	public void StatusErrorSummary_Empty_WhenNone()
	{
		var panel = new MessagePanelViewModel();

		panel.StatusErrorSummary.Should().Be(string.Empty);
	}

	[Fact]
	public void Clear_ResetsCountsAndEntries()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e", "src");
		panel.RefreshReasons(new List<IReason> { new Warning("w") });

		panel.Clear();

		panel.ErrorCount.Should().Be(0);
		panel.WarningCount.Should().Be(0);
		panel.Entries.Should().BeEmpty();
	}

	[Fact]
	public void Clear_SetsHasErrorsFalse()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e", "src");

		panel.Clear();

		panel.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void HasEntries_True_AfterAddError()
	{
		var panel = new MessagePanelViewModel();

		panel.AddError("e", "src");

		panel.HasEntries.Should().BeTrue();
	}

	[Fact]
	public void HasEntries_False_Initially()
	{
		var panel = new MessagePanelViewModel();

		panel.HasEntries.Should().BeFalse();
	}

	[Fact]
	public void ShowPanel_True_WhenHasEntriesAndVisible()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e", "src");
		panel.IsVisible = true;

		panel.ShowPanel.Should().BeTrue();
	}

	[Fact]
	public void ShowPanel_False_WhenNotVisible()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e", "src");
		panel.IsVisible = false;

		panel.ShowPanel.Should().BeFalse();
	}

	[Fact]
	public void RefreshReasons_AddsStructuralErrors()
	{
		var panel = new MessagePanelViewModel();
		var reasons = new List<IReason> { new Error("some error") };

		panel.RefreshReasons(reasons);

		panel.Entries.Should().ContainSingle(e => e.IsStructural && e.IsError);
	}

	[Fact]
	public void RefreshReasons_AddsStructuralWarnings()
	{
		var panel = new MessagePanelViewModel();
		var reasons = new List<IReason> { new Warning("some warning") };

		panel.RefreshReasons(reasons);

		panel.Entries.Should().ContainSingle(e => e.IsStructural && e.IsWarning);
	}

	[Fact]
	public void RefreshReasons_RemovesOldStructuralEntries_BeforeAddingNew()
	{
		var panel = new MessagePanelViewModel();
		panel.RefreshReasons(new List<IReason> { new Error("old error") });

		panel.RefreshReasons(new List<IReason>());

		panel.Entries.Should().BeEmpty();
	}

	[Fact]
	public void RefreshReasons_PreservesNonStructuralEntries()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("non-structural", "custom source");

		panel.RefreshReasons(new List<IReason>());

		panel.Entries.Should().ContainSingle(e => !e.IsStructural);
	}

	[Fact]
	public void ClearCommand_RemovesNonStructuralEntries()
	{
		var panel = new MessagePanelViewModel();
		panel.AddError("e", "src");

		panel.ClearCommand.Execute().Subscribe();

		panel.Entries.Should().BeEmpty();
	}

	[Fact]
	public void ClearCommand_PreservesStructuralEntries()
	{
		var panel = new MessagePanelViewModel();
		panel.RefreshReasons(new List<IReason> { new Error("structural error") });

		panel.ClearCommand.Execute().Subscribe();

		panel.Entries.Should().ContainSingle(e => e.IsStructural);
	}
}
