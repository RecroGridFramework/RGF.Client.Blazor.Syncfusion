using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Abstraction.Infrastructure.Events;
using Recrovit.RecroGridFramework.Abstraction.Models;
using Recrovit.RecroGridFramework.Client.Blazor.Components;
using Syncfusion.Blazor.Grids;
using System;
using System.Linq;

namespace Recrovit.RecroGridFramework.Client.Blazor.SyncfusionUI.Components;

public partial class GridComponent : ComponentBase
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
        GridParameters.Events.CreateAttributes.Subscribe(OnCreateAttributes);
    }

    protected virtual Task OnCreateAttributes(DataEventArgs<RgfDynamicDictionary> rowData)
    {
        foreach (var prop in EntityDesc.SortedVisibleColumns)
        {
            var attr = rowData.Value["__attributes"] as RgfDynamicDictionary;
            if (attr != null)
            {
                string? propAttr = null;
                if (prop.FormType == PropertyFormType.CheckBox)
                {
                    propAttr = " text-center";
                }
                else if (prop.ListType == PropertyListType.Numeric)
                {
                    propAttr = " text-end";
                }
                if (propAttr != null)
                {
                    attr[$"class-{prop.Alias}"] += propAttr;
                }
            }
        }
        return Task.CompletedTask;
    }

    protected virtual Task RowDataBound(RowDataBoundEventArgs<RgfDynamicDictionary> args)
    {
        var rowData = args.Data;
        var attributes = (RgfDynamicDictionary)rowData["__attributes"];
        var attr = attributes.GetItemData("class").StringValue;
        if (attr != null)
        {
            args.Row.AddClass(attr.Split(' ').ToArray());
        }
        attr = attributes.GetItemData("style").StringValue;
        if (attr != null)
        {
            args.Row.AddStyle(attr.Split(';').ToArray());
        }
        return Task.CompletedTask;
    }

    protected virtual Task CustomizeCell(QueryCellInfoEventArgs<RgfDynamicDictionary> args)
    {
        var prop = EntityDesc.Properties.SingleOrDefault(e => e.Alias == args.Column.Field);
        if (prop?.ColPos > 0)
        {
            var rowData = args.Data;
            var attributes = (RgfDynamicDictionary)rowData["__attributes"];
            var attr = attributes.GetItemData($"class-{prop.Alias}").StringValue;
            if (attr != null)
            {
                args.Cell.AddClass(attr.Split(' ').ToArray());
            }
            attr = attributes.GetItemData($"style-{prop.Alias}").StringValue;
            if (attr != null)
            {
                args.Cell.AddStyle(attr.Split(';').ToArray());
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

    protected virtual Task RowSelectHandler(RowSelectEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.RowSelectHandlerAsync(args.Data);

    protected virtual Task RowDeselectHandler(RowDeselectEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.RowDeselectHandlerAsync(args.Data);

    protected virtual Task OnRecordDoubleClick(RecordDoubleClickEventArgs<RgfDynamicDictionary> args) => _rgfGridRef.OnRecordDoubleClickAsync(args.RowData);
}
