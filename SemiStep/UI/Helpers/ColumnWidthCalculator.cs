using System.Globalization;

using Avalonia.Controls;
using Avalonia.Media;

using Shared.Config;
using Shared.Config.Contracts;
using Shared.Style;

namespace UI.Helpers;

public sealed class ColumnWidthCalculator(
	IActionRegistry actionRegistry,
	IGroupRegistry groupRegistry,
	GridStyleOptions gridStyle)
{
	private const string ActionComboBox = "action_combo_box";
	private const string ActionTargetComboBox = "action_target_combo_box";
	private const string PropertyField = "property_field";
	private const string StepStartTimeField = "step_start_time_field";
	private const string TextField = "text_field";

	private const string RepresentativeTimeValue = "00:00:00";
	private const double BufferMultiplier = 1.4;

	public DataGridLength CalculateColumnWidth(GridColumnDefinition columnDef)
	{
		return columnDef.ColumnType.ToLowerInvariant() switch
		{
			ActionComboBox => CalculateActionColumnWidth(columnDef),
			ActionTargetComboBox => CalculateGroupColumnWidth(columnDef),
			PropertyField => CalculateHeaderBasedWidth(columnDef),
			StepStartTimeField => CalculateTimeColumnWidth(columnDef),
			TextField => new DataGridLength(1, DataGridLengthUnitType.Star),
			_ => CalculateHeaderBasedWidth(columnDef)
		};
	}

	private DataGridLength CalculateActionColumnWidth(GridColumnDefinition columnDef)
	{
		var actionNames = actionRegistry.GetAll().Select(a => a.UiName);

		return CalculateWidth(columnDef.UiName, actionNames);
	}

	private DataGridLength CalculateGroupColumnWidth(GridColumnDefinition columnDef)
	{
		var displayStrings = CollectGroupDisplayStrings(columnDef.Key);

		return CalculateWidth(columnDef.UiName, displayStrings);
	}

	private DataGridLength CalculateHeaderBasedWidth(GridColumnDefinition columnDef)
	{
		return CalculateWidth(columnDef.UiName, []);
	}

	private DataGridLength CalculateTimeColumnWidth(GridColumnDefinition columnDef)
	{
		return CalculateWidth(columnDef.UiName, [RepresentativeTimeValue]);
	}

	private DataGridLength CalculateWidth(string headerText, IEnumerable<string> contentStrings)
	{
		var headerWidth = CompensateThemeSortIconAndPaddingOffset(headerText);

		var maxContentWidth = 0.0;
		foreach (var text in contentStrings)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}

			var contentWidth = MeasureText(text, gridStyle.CellFontSize);
			if (contentWidth > maxContentWidth)
			{
				maxContentWidth = contentWidth;
			}
		}

		var maxWidth = Math.Max(headerWidth, maxContentWidth);
		var pixelWidth = (int)Math.Ceiling(maxWidth * BufferMultiplier);

		return new DataGridLength(pixelWidth);
	}

	private IEnumerable<string> CollectGroupDisplayStrings(string columnKey)
	{
		var groupNames = new HashSet<string>();

		foreach (var action in actionRegistry.GetAll())
		{
			var actionColumn = action.Columns.FirstOrDefault(c => c.Key == columnKey);
			if (actionColumn?.GroupName is not null)
			{
				groupNames.Add(actionColumn.GroupName);
			}
		}

		foreach (var groupName in groupNames)
		{
			if (!groupRegistry.GroupExists(groupName))
			{
				continue;
			}

			var group = groupRegistry.GetGroup(groupName);
			foreach (var item in group.Items.Values)
			{
				yield return item;
			}
		}
	}

	private double CompensateThemeSortIconAndPaddingOffset(string headerText)
	{
		const double FluentThemeSortIconMinWidth = 32;

		var textWidth = MeasureText(headerText, gridStyle.HeaderFontSize);

		return textWidth + FluentThemeSortIconMinWidth;
	}

	private static double MeasureText(string text, double fontSize)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		var typeface = new Typeface(FontFamily.Default);
		var formattedText = new FormattedText(
			text,
			CultureInfo.CurrentCulture,
			FlowDirection.LeftToRight,
			typeface,
			fontSize,
			Brushes.Black);

		return formattedText.WidthIncludingTrailingWhitespace;
	}
}
