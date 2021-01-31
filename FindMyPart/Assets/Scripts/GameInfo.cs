public class GameInfo {
    public int Width { get; set; }
    public int Height { get; set; }

    public float RealWidth { get; set; }
    public float RealHeight { get; set; }

    public float XLeft { get; set; }
    public float ZTop { get; set; }

    public float TileWidth => this.RealWidth / this.Width;
    public float TileHeight => this.RealHeight / this.Height;
}
