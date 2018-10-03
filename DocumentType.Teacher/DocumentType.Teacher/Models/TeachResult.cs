namespace DocumentType.Teacher.Models
{
    public class TeachResult
    {
        public int Iteration { get; set; }

        public double Error { get; set; }

        public int Successes { get; set; }
        
        public double SuccessPercent { get; set; }
        
        public int ImageIndex { get; set; }
        
        public int Target { get; set; }
    }
}
