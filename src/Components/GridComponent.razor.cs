using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Abstraction.Contracts.Services;
using Recrovit.RecroGridFramework.Abstraction.Models;
using Recrovit.RecroGridFramework.Client.Blazor.Components;
using Recrovit.RecroGridFramework.Client.Events;
using Recrovit.RecroGridFramework.Client.Handlers;
using Syncfusion.Blazor.Grids;

namespace Recrovit.RecroGridFramework.Client.Blazor.SyncfusionUI.Components;

public partial class GridComponent : ComponentBase, IDisposable
{
    [Inject]
    private ILogger<GridComponent> _logger { get; set; } = null!;

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = null!;

    private RgfGridComponent _rgfGridRef { get; set; } = null!;

    private SfGrid<RgfDynamicDictionary> _sfGridRef { get; set; } = null!;

    private RgfEntity EntityDesc => Manager.EntityDesc;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GridParameters.EnableMultiRowSelection = false;
        GridParameters.EventDispatcher.Subscribe(RgfListEventKind.CreateRowData, OnCreateAttributes);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            _rgfGridRef.EntityParameters.ToolbarParameters.EventDispatcher.Subscribe([RgfToolbarEventKind.Read, RgfToolbarEventKind.Edit], OnSetFormItem);
        }
    }

    public void Dispose()
    {
        GridParameters.EventDispatcher.Unsubscribe(RgfListEventKind.CreateRowData, OnCreateAttributes);
        _rgfGridRef.EntityParameters.ToolbarParameters.EventDispatcher.Unsubscribe([RgfToolbarEventKind.Read, RgfToolbarEventKind.Edit], OnSetFormItem);
    }

    protected virtual Task OnCreateAttributes(IRgfEventArgs<RgfListEventArgs> arg)
    {
        _logger.LogDebug("CreateAttributes");
        var rowData = arg.Args.Data ?? throw new ArgumentException();
        foreach (var prop in Manager.ListHandler.SortedVisibleColumns)
        {
            string? propClass = null;
            if (prop.FormType == PropertyFormType.CheckBox)
            {
                propClass = "text-center";
            }
            else if (prop.ListType == PropertyListType.Numeric)
            {
                propClass = "text-end";
            }
            if (propClass != null)
            {
                var attributes = rowData.GetOrNew<RgfDynamicDictionary>("__attributes");
                var propAttributes = attributes.GetOrNew<RgfDynamicDictionary>(prop.Alias);
                propAttributes.Set<string>("class", (old) => string.IsNullOrEmpty(old) ? propClass : $"{old.Trim()} {propClass}");
            }
        }
        return Task.CompletedTask;
    }

    protected virtual Task RowDataBound(RowDataBoundEventArgs<RgfDynamicDictionary> args)
    {
        var rowData = args.Data;
        var attributes = rowData.Get<RgfDynamicDictionary>("__attributes");
        if (attributes != null)
        {
            var attr = attributes.Get<string>("class");
            if (attr != null)
            {
                args.Row.AddClass(attr.Split(' ').ToArray());
            }
            attr = attributes.Get<string>("style");
            if (attr != null)
            {
                args.Row.AddStyle(attr.Split(';').ToArray());
            }
        }
        return Task.CompletedTask;
    }

    protected virtual Task CustomizeCell(QueryCellInfoEventArgs<RgfDynamicDictionary> args)
    {
        var prop = EntityDesc.Properties.SingleOrDefault(e => e.Alias == args.Column.Field);
        if (prop?.ColPos > 0)
        {
            var rowData = args.Data;
            var attributes = rowData.Get<RgfDynamicDictionary>("__attributes");
            if (attributes != null)
            {
                var propAttributes = attributes.Get<RgfDynamicDictionary>(prop.Alias);
                if (propAttributes != null)
                {
                    var attr = propAttributes.Get<string>("class");
                    if (attr != null)
                    {
                        args.Cell.AddClass(attr.Split(' ').ToArray());
                    }
                    attr = propAttributes.Get<string>("style");
                    if (attr != null)
                    {
                        args.Cell.AddStyle(attr.Split(';').ToArray());
                    }
                }
            }
        }
        return Task.CompletedTask;
    }

    protected virtual async Task OnActionBegin(ActionEventArgs<RgfDynamicDictionary> args)
    {
        _logger.LogDebug("ActionBegin: {RequestType}", args.RequestType);
        if (args.RequestType.Equals(Syncfusion.Blazor.Grids.Action.Sorting))
        {
            await Manager.ListHandler.SetSortAsync(_sfGridRef.SortSettings.Columns
                .Select((column, i) => new { Alias = column.Field, Sort = column.Direction == SortDirection.Ascending ? i + 1 : -(i + 1) })
                .ToDictionary(e => e.Alias, e => e.Sort));
        }
        else if (args.RequestType.Equals(Syncfusion.Blazor.Grids.Action.Reorder))
        {
            int from = args.FromColumns[0].OriginalIndex + 1;
            int to = args.ToColumn.Index + 1;
            args.Cancel = true;
            _ = Task.Run(async () => await Manager.ListHandler.MoveColumnAsync(from, to));
        }
    }

    protected virtual async Task RowSelectHandler(RowSelectEventArgs<RgfDynamicDictionary> args)
    {
        var rowData = args.Data;
        if (_rgfGridRef.SelectedItems.Any())
        {
            int idx = Manager.ListHandler.GetAbsoluteRowIndex(rowData);
            bool deselect = _rgfGridRef.SelectedItems.ContainsKey(idx);
            await _rgfGridRef.RowDeselectHandlerAsync(rowData);
            if (deselect)
            {
                return;
            }
        }
        await _rgfGridRef.RowSelectHandlerAsync(rowData);
    }

    protected virtual Task RowDeselectHandler(RowDeselectEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.RowDeselectHandlerAsync(args.Data);

    protected virtual Task OnRecordDoubleClick(RecordDoubleClickEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.OnRecordDoubleClickAsync(args.RowData);

    private void OnSetFormItem(IRgfEventArgs<RgfToolbarEventArgs> arg)
    {
        var data = _rgfGridRef.SelectedItems.Single();
        int rowIndex = Manager.ListHandler.ToRelativeRowIndex(data.Key);
        if (rowIndex != -1)
        {
            _sfGridRef.ClearSelectionAsync();
            _sfGridRef.SelectRowAsync(rowIndex);
        }
    }
}