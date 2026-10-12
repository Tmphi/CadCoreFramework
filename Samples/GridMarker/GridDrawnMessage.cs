namespace CadCoreFramework.Samples.GridMarker
{
    /// <summary>
    /// GOLDEN SAMPLE — MESSAGE (EventAggregator).
    ///
    /// Được publish SAU KHI transaction vẽ lưới đã commit thành công, bất kể lưới được vẽ
    /// từ Command hay từ Palette. Ai quan tâm (ví dụ GridMarkerViewModel) tự subscribe —
    /// Service không cần biết có những ai đang nghe.
    /// </summary>
    public sealed class GridDrawnMessage
    {
        public int CellCount { get; }
        public string LayerName { get; }

        public GridDrawnMessage(int cellCount, string layerName)
        {
            CellCount = cellCount;
            LayerName = layerName;
        }
    }
}
