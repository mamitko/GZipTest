namespace GZipTest.Threading
{
    /// <summary>
    /// � ��������� ���� �� ������������!
    /// ����� ����� ������ ������������� � ����������������� ����� 
    /// � �� ������������� ������������ ���������� (��� �������� ��� bool, � ��� ������).
    /// </summary>
    internal sealed class ReferenceBool
    {
        private static readonly ReferenceBool TrueInstance = new ReferenceBool();

        private ReferenceBool() { }

        public static implicit operator bool(ReferenceBool value)
        {
            return value != null;
        }

        public static implicit operator ReferenceBool(bool value)
        {
            return value ? TrueInstance : null; 
        }
    }
}