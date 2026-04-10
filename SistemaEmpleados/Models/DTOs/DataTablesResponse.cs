namespace SistemaEmpleados.Models.DTOs;

public class DataTablesResponse
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public object Data { get; set; } = new List<object>();
}