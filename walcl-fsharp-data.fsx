#r "nuget: FSharp.Data, 6.2.0"
open System.IO
open System.Net.Http
open FSharp.Data

type WALCLRecord = {
    DATE : string
    WALCL : float
}

// type WALCLRecord() =
//     member val DATE = "" with get, set
//     member val WALCL = "" with get, set

let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "WALCL" "2023-01-01"
        
let str = (new HttpClient()).GetStringAsync(uri).Result

// type Walcl = CsvProvider<WALCLRecord>



use string_reader = new StringReader(str)
use csv_reader = new CsvReader(string_reader, System.Globalization.CultureInfo.InvariantCulture)

let records = csv_reader.GetRecords<WALCLRecord>()

let arr = records |> Array.ofSeq

arr[0].WALCL