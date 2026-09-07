namespace RPNCalc.Lib;

internal static class Extensions
{
    extension<T>(IEnumerable<T> self)
    {
        internal IEnumerable<(T item, int index)> WithIndex()
            => self.Select((item, index) => (item, index));
    }
}
