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
// ----------------------------------------------------------------------
// let get_recent_tga (days : int) =

//     let base_uri = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/dts/dts_table_1?"
//     let path = "tga.json"

//     if File.Exists(path) then         
//         let data = JsonSerializer.Deserialize<TGARecordData[]>(File.ReadAllText(path))        
//         let last_date = data[data.Length-1].record_date
//         printfn "Retrieving records since: %s" last_date
//         let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri last_date
//         let result_alt = (new HttpClient()).GetFromJsonAsync<TGARecord>(uri).Result

//         if result_alt.data.Length > 0 then
//             printfn "New records retrieved: %d" result_alt.data.Length
//             let new_data = Array.append data result_alt.data
//             File.WriteAllText(path, JsonSerializer.Serialize new_data)
//             new_data
//         else
//             printfn "No new records retrieved"
//             data        
//     else
//         printfn "Retrieving TGA data"
//         let date = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd")
//         let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri date    
//         let data = (new HttpClient()).GetFromJsonAsync<TGARecord>(uri).Result.data
//         printfn "Retrieved %d records" data.Length
//         File.WriteAllText(path, JsonSerializer.Serialize data)
//         data


let fred_sp500 date =

    let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
    
    config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()    

    let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "SP500" date
    
    let str = (new HttpClient()).GetStringAsync(uri).Result
    
    (new CsvReader(new StringReader(str), config)).GetRecords<SP500RecordRaw>() |> Array.ofSeq

let get_recent_sp500 date =    
    let path = "sp500.json"
        
    if File.Exists(path) then         

        let data = JsonSerializer.Deserialize<SP500RecordRaw[]>(File.ReadAllText(path))        
        let last_date = data[data.Length-1].DATE
        printfn "Retrieving records since: %s" last_date        
        let result_alt = fred_sp500 last_date
        printfn "Retrieved %d records" result_alt.Length
                
        if result_alt.Length > 0 then
            let new_data = Array.append data result_alt
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



// let get_recent_sp500 date =    
//     let path = "sp500.json"
    
//     let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
//     config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()    

//     if File.Exists(path) then         

//         let data = JsonSerializer.Deserialize<SP500RecordRaw[]>(File.ReadAllText(path))        
//         let last_date = data[data.Length-1].DATE
//         printfn "Retrieving records since: %s" last_date
//         let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "SP500" last_date
//         let str = (new HttpClient()).GetStringAsync(uri).Result
//         let result_alt = (new CsvReader(new StringReader(str), config)).GetRecords<SP500RecordRaw>() |> Array.ofSeq
//         printfn "Retrieved %d records" result_alt.Length
                
//         if result_alt.Length > 0 then
//             let new_data = Array.append data result_alt
//             File.WriteAllText(path, JsonSerializer.Serialize new_data)
//             new_data
//         else
//             data        
//     else
//         printfn "Retrieving data"
        
//         let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "SP500" date    
//         let str = (new HttpClient()).GetStringAsync(uri).Result
//         let data = (new CsvReader(new StringReader(str), config)).GetRecords<SP500RecordRaw>() |> Array.ofSeq
        
//         printfn "Retrieved %d records" data.Length
//         File.WriteAllText(path, JsonSerializer.Serialize data)
//         data



let result_a = get_recent_sp500 "2023-01-01"


let get_sp500 date =



    let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)

    config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()

    let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" "SP500" date
            
    let str = (new HttpClient()).GetStringAsync(uri).Result
    
    let arr = (new CsvReader(new StringReader(str), config)).GetRecords<SP500RecordRaw>() |> 
        Array.ofSeq |> 
        Array.filter (fun elt -> elt.SP500 <> ".") |> 
        Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })

    arr



let date = "2023-01-01"

let arr = get_sp500 date

for elt in arr do
    printfn "%10s %15f" elt.date elt.value


// for elt in arr do
//     printfn "%10s %15s" elt.DATE elt.SP500

// let result = arr |> Array.filter (fun elt -> elt.SP500 <> ".") |> Array.map (fun elt -> { date = elt.DATE; value = float elt.SP500 })

exit 0

// let result_alt = arr |> Array.where (fun elt -> elt.SP500 = ".")

// result_alt |> format_table