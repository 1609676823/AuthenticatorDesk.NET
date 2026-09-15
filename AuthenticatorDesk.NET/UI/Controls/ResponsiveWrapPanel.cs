namespace AuthenticatorDesk.UI.Controls;

public sealed class ResponsiveWrapPanel : Panel
{
    private bool _updatingLayout;
    private int _minimumItemWidth = 180;
    private int _itemHeight = 36;
    private int _maximumColumns = 2;
    private int _horizontalSpacing = 10;
    private int _verticalSpacing = 8;

    [System.ComponentModel.DefaultValue(180)]
    public int MinimumItemWidth
    {
        get => _minimumItemWidth;
        set
        {
            _minimumItemWidth = Math.Max(1, value);
            PerformLayout();
        }
    }

    [System.ComponentModel.DefaultValue(36)]
    public int ItemHeight
    {
        get => _itemHeight;
        set
        {
            _itemHeight = Math.Max(1, value);
            PerformLayout();
        }
    }

    [System.ComponentModel.DefaultValue(2)]
    public int MaximumColumns
    {
        get => _maximumColumns;
        set
        {
            _maximumColumns = Math.Max(1, value);
            PerformLayout();
        }
    }

    [System.ComponentModel.DefaultValue(10)]
    public int HorizontalSpacing
    {
        get => _horizontalSpacing;
        set
        {
            _horizontalSpacing = Math.Max(0, value);
            PerformLayout();
        }
    }

    [System.ComponentModel.DefaultValue(8)]
    public int VerticalSpacing
    {
        get => _verticalSpacing;
        set
        {
            _verticalSpacing = Math.Max(0, value);
            PerformLayout();
        }
    }

    protected override void OnControlAdded(ControlEventArgs eventArgs)
    {
        base.OnControlAdded(eventArgs);
        PerformLayout();
    }

    protected override void OnControlRemoved(ControlEventArgs eventArgs)
    {
        base.OnControlRemoved(eventArgs);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs eventArgs)
    {
        base.OnLayout(eventArgs);
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        if (_updatingLayout)
        {
            return;
        }

        _updatingLayout = true;
        try
        {
            var children = Controls
                .Cast<Control>()
                .Where(control => control.Visible)
                .ToArray();
            var scale = DeviceDpi / 96F;
            int S(int value) => value == 0
                ? 0
                : Math.Max(1, (int)Math.Round(value * scale));
            var minimumItemWidth = S(MinimumItemWidth);
            var itemHeight = S(ItemHeight);
            var horizontalSpacing = S(HorizontalSpacing);
            var verticalSpacing = S(VerticalSpacing);
            var innerWidth = Math.Max(1, ClientSize.Width - Padding.Horizontal);
            var columnsByWidth = Math.Max(
                1,
                (innerWidth + horizontalSpacing) /
                (minimumItemWidth + horizontalSpacing));
            var columns = Math.Min(
                Math.Min(MaximumColumns, columnsByWidth),
                Math.Max(1, children.Length));
            var itemWidth = Math.Max(
                1,
                (innerWidth - ((columns - 1) * horizontalSpacing)) / columns);

            for (var index = 0; index < children.Length; index++)
            {
                var row = index / columns;
                var column = index % columns;
                children[index].SetBounds(
                    Padding.Left + (column * (itemWidth + horizontalSpacing)),
                    Padding.Top + (row * (itemHeight + verticalSpacing)),
                    itemWidth,
                    itemHeight);
            }

            var rows = children.Length == 0
                ? 0
                : (children.Length + columns - 1) / columns;
            var preferredHeight = Padding.Vertical +
                                  (rows * itemHeight) +
                                  (Math.Max(0, rows - 1) * verticalSpacing);
            if (Height != preferredHeight)
            {
                Height = preferredHeight;
            }
        }
        finally
        {
            _updatingLayout = false;
        }
    }
}
