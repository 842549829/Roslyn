namespace Model
{
    /// <summary>
    /// Order
    /// </summary>
    public class Order
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Number { get; set; }

        public decimal Amount { get; set; }
    }
}