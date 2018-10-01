namespace ImageProcessing
{
    public class Cords
    {
        public Cords()
        {
        }

        public Cords(int top, int bottom, int left, int right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }
        
        public int Top { get; set; }

        public int Bottom { get; set; }
        
        public int Right { get; set; }
        
        public int Left { get; set; }
        
        public override bool Equals(object obj)
        {
            var item = obj as Cords;

            if (item == null)
            {
                return false;
            }

            return 
                this.Top.Equals(item.Top) && 
                this.Right.Equals(item.Right) && 
                this.Left.Equals(item.Left) && 
                this.Bottom.Equals(item.Bottom);
        }
        
        public override int GetHashCode()
        {
            return this.Top.GetHashCode() ^ 
                   this.Bottom.GetHashCode() ^ 
                   this.Left.GetHashCode() ^ 
                   this.Right.GetHashCode();
        }
    }
}