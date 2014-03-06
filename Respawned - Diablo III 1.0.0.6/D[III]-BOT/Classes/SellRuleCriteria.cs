
namespace Respawned
{
    public class SellRuleCriteria
    {
        private int _statID;
        private decimal _minAmount;

        public SellRuleCriteria()
        {
        }
        public SellRuleCriteria(int statID, decimal minAmount)
        {
            StatID = statID;
            MinAmount = minAmount;
        }

        public int StatID
        {
            get { return _statID; }
            set { _statID = value; }
        }

        public decimal MinAmount
        {
            get { return _minAmount; }
            set { _minAmount = value; }
        }
    }
}
