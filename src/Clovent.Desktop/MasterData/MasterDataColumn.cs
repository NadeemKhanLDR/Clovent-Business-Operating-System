namespace Clovent.Desktop.MasterData;

/// <summary>
/// One column of a <see cref="MasterDataListView{TDto}"/> grid. Maps
/// directly onto <c>DevExpress.XtraGrid.Views.Grid.GridView</c>'s
/// reflection-based binding: <see cref="FieldName"/> must name a public
/// property on the bound DTO record.
/// </summary>
/// <param name="FieldName">The DTO property name to bind.</param>
/// <param name="Caption">The column header text.</param>
/// <param name="Width">An explicit column width, or <see langword="null"/> to let the grid size it automatically.</param>
public sealed record MasterDataColumn(string FieldName, string Caption, int? Width = null);
