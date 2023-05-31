#r "nuget: CsvHelper, 30.0.1"

open System
open System.IO
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json
open CsvHelper
// ----------------------------------------------------------------------
type SP500RecordRaw = {
    DATE  : string
    SP500 : string
}

type SP500Record = {
    date  : string
    value : float
}
let fred_sp500 date =

    let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
    
    config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()    

    let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "SP500" date
    
    let str = (new HttpClient()).GetStringAsync(uri).Result
    
    (new CsvReader(new StringReader(str), config)).GetRecords<SP500RecordRaw>() |> Array.ofSeq
// ----------------------------------------------------------------------
let get_recent_sp500 date =    
    let path = "sp500.json"
        
    if File.Exists(path) then         

        let data = JsonSerializer.Deserialize<SP500RecordRaw[]>(File.ReadAllText(path))        
        let last_date = data[data.Length-1].DATE
        // let since = DateTime.Parse(last_date).AddDays(1).ToString("yyyy-MM-dd")
        printfn "Retrieving records since: %s" last_date
        let result_alt = fred_sp500 last_date
        printfn "Retrieved %d records" result_alt.Length

        let to_add = result_alt |> Array.where (fun elt -> elt.DATE > last_date)

        printfn "Adding %d records" to_add.Length
                
        if to_add.Length > 0 then
            let new_data = Array.append data to_add
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            data        
    else
        printfn "Retrieving data"
        let data = fred_sp500 date
        printfn "Retrieved %d records" data.Length
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data
// ----------------------------------------------------------------------
let get_sp500 date =
    get_recent_sp500 date |> 
    Array.filter (fun elt -> elt.SP500 <> ".") |> 
    Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })
// ----------------------------------------------------------------------

let result_raw = get_recent_sp500 "2023-01-01"
// ----------------------------------------------------------------------

for elt in result_raw do
    printfn "%10s %15s" elt.DATE elt.SP500


let result = result_raw |> Array.filter (fun elt -> elt.SP500 <> ".") |> Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })

for elt in result do
    printfn "%10s %15f" elt.date elt.value


// let date = "2023-01-01"

// let arr = get_sp500 date

// for elt in arr do
//     printfn "%10s %15f" elt.date elt.value


// for elt in arr do
//     printfn "%10s %15s" elt.DATE elt.SP500

// let result = arr |> Array.filter (fun elt -> elt.SP500 <> ".") |> Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })

let result_b = get_sp500 "2023-01-01"

result_b |> format_table

exit 0

// let result_alt = arr |> Array.where (fun elt -> elt.SP500 = ".")

// result_alt |> format_table

let xyz = [| for i in 1 .. 10000 -> i |]

let test_files = Directory.GetFiles("c:/Windows/System32")