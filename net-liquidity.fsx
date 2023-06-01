#r "nuget: CsvHelper, 30.0.1"
#r "nuget: FSharp.Json, 0.4.1"
#r "nuget: Fli, 1.0.1"

open System
open System.IO
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json
open FSharp.Json
open Fli
open CsvHelper

fsi.ShowDeclarationValues <- false
// ----------------------------------------------------------------------
let format_table seq =
    let json = JsonSerializer.Serialize seq
    System.IO.File.WriteAllText("c:/temp/out.json", json)
    let result_cli = (cli {
        Shell PS
        Command "Get-Content c:\\temp\\out.json | ConvertFrom-Json | Format-Table *"
    } |> Command.execute)

    match result_cli.Text with
    | Some(txt) -> printfn "%s" txt
    | None -> printfn "issue"
// ----------------------------------------------------------------------
let days = 365
// ----------------------------------------------------------------------
type TGARecordData = {
    record_date           : string
    open_today_bal        : int64
}

type TGARecord = {
    data  : TGARecordData[]
}

let get_recent_tga (days : int) =

    let base_uri = "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/dts/dts_table_1?"
    let path = "tga.json"

    if File.Exists(path) then         
        let data = JsonSerializer.Deserialize<TGARecordData[]>(File.ReadAllText(path))        
        let last_date = data[data.Length-1].record_date
        printfn "Retrieving records since: %s" last_date
        let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri last_date
        let result_alt = (new HttpClient()).GetFromJsonAsync<TGARecord>(uri).Result

        if result_alt.data.Length > 0 then
            printfn "New records retrieved: %d" result_alt.data.Length
            let new_data = Array.append data result_alt.data
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            printfn "No new records retrieved"
            data        
    else
        printfn "Retrieving TGA data"
        let date = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd")
        let uri = sprintf "%sfilter=record_date:gt:%s,account_type:eq:Treasury General Account (TGA) Closing Balance&fields=record_date,open_today_bal&page[number]=1&page[size]=300" base_uri date    
        let data = (new HttpClient()).GetFromJsonAsync<TGARecord>(uri).Result.data
        printfn "Retrieved %d records" data.Length
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data
// ----------------------------------------------------------------------
type RRPRecordRepoOperation = {
    operationId      : string
    operationDate    : string
    operationType    : string
    note             : string
    totalAmtAccepted : int64
}

type RRPRecordRepo = {
    operations : RRPRecordRepoOperation[]
}

type RRPRecord = {
    repo : RRPRecordRepo
}

let get_recent_rrp (days : int) =
    
    let path = "rrp.json"

    if File.Exists(path) then         
        let data = JsonSerializer.Deserialize<RRPRecordRepoOperation[]>(File.ReadAllText(path))
        let last_date = data[data.Length-1].operationDate
        let date = DateTime.Parse(last_date).AddDays(1).ToString("yyyy-MM-dd")
        printfn "Retrieving records since: %s" date
        let uri = sprintf "https://markets.newyorkfed.org/api/rp/reverserepo/propositions/search.json?startDate=%s" date
        let result_alt = (new HttpClient()).GetFromJsonAsync<RRPRecord>(uri).Result.repo.operations |> Array.sortBy (fun item -> item.operationDate)

        if result_alt.Length > 0 then
            printfn "New records retrieved: %d" result_alt.Length
            let new_data = Array.append data result_alt
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            printfn "No new records retrieved"
            data        
    else
        printfn "Retrieving RRP data"
        let date = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd")
        let uri = sprintf "https://markets.newyorkfed.org/api/rp/reverserepo/propositions/search.json?startDate=%s" date
        let data = (new HttpClient()).GetFromJsonAsync<RRPRecord>(uri).Result.repo.operations |> Array.sortBy (fun item -> item.operationDate)
        printfn "Retrieved %d records" data.Length       
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data
// ----------------------------------------------------------------------
type FREDRecordRaw = {
    date : string
    value : string
}

type FREDRecord = {
    date : string
    value : decimal
}

let download_fred_series series date =

    printfn "Retrieving records since: %s" date

    let uri = sprintf  "https://fred.stlouisfed.org/graph/fredgraph.csv?id=%s&cosd=%s" series date
        
    let str = (new HttpClient()).GetStringAsync(uri).Result
        
    let new_str = String.Join(Environment.NewLine, 
        Array.append [| "date,value" |]  (str.Split(Environment.NewLine.ToCharArray()) |> Array.skip 1))

    let config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        
    config.PrepareHeaderForMatch <- fun args -> args.Header.ToLower()    

    let data = (new CsvReader(new StringReader(new_str), config)).GetRecords<FREDRecordRaw>() |> Array.ofSeq

    printfn "Retrieved %d records" data.Length

    data

let get_fred_series_raw series date =
    let path = sprintf "%s.json" series
        
    if File.Exists(path) then         

        let data = JsonSerializer.Deserialize<FREDRecordRaw[]>(File.ReadAllText(path))        
        let last_date = data[data.Length-1].date
        let result_alt = download_fred_series series last_date

        let to_add = result_alt |> Array.where (fun elt -> elt.date > last_date)

        printfn "Adding %d records" to_add.Length
                
        if to_add.Length > 0 then
            let new_data = Array.append data to_add
            File.WriteAllText(path, JsonSerializer.Serialize new_data)
            new_data
        else
            data        
    else
        let data = download_fred_series series date
        File.WriteAllText(path, JsonSerializer.Serialize data)
        data

let get_fred_series series date =
    get_fred_series_raw series date |>
    Array.filter (fun elt -> elt.value <> ".") |> 
    Array.map (fun elt -> { date = elt.date; value = decimal elt.value })    
// ----------------------------------------------------------------------
let result_tga = get_recent_tga days
let result_rrp = get_recent_rrp days
let result_fed = get_fred_series "WALCL" "2022-01-01"
let result_spx = get_fred_series "SP500" "2022-01-01"
// ----------------------------------------------------------------------
let earliest = Array.concat [
    (result_tga |> Array.map (fun elt -> elt.record_date)   |> Array.take 1)
    (result_rrp |> Array.map (fun elt -> elt.operationDate) |> Array.take 1)
    (result_fed |> Array.map (fun elt -> elt.date)          |> Array.take 1)
    (result_spx |> Array.map (fun elt -> elt.date)          |> Array.take 1) ] |> Array.max

let dates_union = Array.concat [
    result_tga |> Array.map (fun elt -> elt.record_date)
    result_rrp |> Array.map (fun elt -> elt.operationDate)
    result_fed |> Array.map (fun elt -> elt.date)
    result_spx |> Array.map (fun elt -> elt.date) 
] 

let dates = dates_union |> Array.sort |> Array.distinct |> Array.where (fun elt -> elt >= earliest) 
// ----------------------------------------------------------------------
type TableEntry = {
    date     : string
    fed      : decimal
    rrp      : decimal
    tga      : decimal
    nl       : decimal
    spx      : decimal
    spx_fv   : decimal
    spx_low  : decimal
    spx_high : decimal
    
    fed_change : decimal
    rrp_change : decimal
    tga_change : decimal
    nl_change  : decimal    
}

let table = dates |> Array.map (fun date -> 

    let tga_record = result_tga |> Array.findBack (fun elt -> elt.record_date <= date)
    let rrp_record = result_rrp |> Array.findBack (fun elt -> elt.operationDate <= date)
    let fed_record = result_fed |> Array.findBack (fun elt -> elt.date <= date)
    let spx_record = result_spx |> Array.findBack (fun elt -> elt.date <= date)

    let fed = fed_record.value * 1000M * 1000M
    let rrp = decimal rrp_record.totalAmtAccepted
    let tga = decimal (tga_record.open_today_bal * 1000L * 1000L)

    let nl = fed - rrp - tga

    let spx = spx_record.value |> Math.Round

    let spx_fv = nl / 1000M / 1000M / 1000M / 1.1M - 1625M |> Math.Round
    let spx_low = spx_fv - 150M
    let spx_high = spx_fv + 350M

    {
        date     = date
        fed      = fed
        rrp      = rrp
        tga      = tga
        nl       = nl
        spx      = spx
        spx_fv   = spx_fv
        spx_low  = spx_low
        spx_high = spx_high

        fed_change = 0M 
        rrp_change = 0M
        tga_change = 0M
        nl_change  = 0M
    }

)

let a = Array.skip 1 table
let b = Array.take (table.Length - 1) table

let calc_changes = Array.map2 (fun curr prev ->    
    { 
        curr with 
            fed_change = curr.fed - prev.fed 
            rrp_change = curr.rrp - prev.rrp
            tga_change = curr.tga - prev.tga
            nl_change  = curr.nl  - prev.nl
    }
)

let table_with_changes = calc_changes a b
// ----------------------------------------------------------------------

let val_to_color (value : decimal) =
    if value > 0M then 
        ConsoleColor.Green
    else if value < 0M then
        ConsoleColor.Red
    else
        ConsoleColor.White

let write format (item : string) color =
    Console.ForegroundColor <- color
    Console.Write(format, item)
    Console.ResetColor()

let header = "DATE                     WALCL              CHANGE                 RRP              CHANGE                 TGA              CHANGE       NET LIQUIDITY              CHANGE    SPX FV"

printfn "%s" header

table_with_changes |> Array.iter (fun elt ->

    printf "%s" elt.date
    printf "%20s" (elt.fed.ToString("N0"))
    write "{0,20}" (elt.fed_change.ToString("N0")) (val_to_color elt.fed_change)
    printf "%20s" (elt.rrp.ToString("N0"))
    write "{0,20}" (elt.rrp_change.ToString("N0")) (val_to_color elt.rrp_change)
    printf "%20s" (elt.tga.ToString("N0"))
    write "{0,20}" (elt.tga_change.ToString("N0")) (val_to_color elt.tga_change)
    printf "%20s" (elt.nl.ToString("N0"))
    write "{0,20}" (elt.nl_change.ToString("N0")) (val_to_color elt.nl_change)
    printf "%10s" (elt.spx_fv.ToString("N0"))
    printfn ""
    ()
)

printfn "%s" header

// ----------------------------------------------------------------------
let item = {|
    chart = {|
        ``type`` = "bar"
        data = {|
            labels = table_with_changes |> Array.map (fun elt -> elt.date )
            datasets = [|
                {| label = "NL";    data = table_with_changes |> Array.map (fun elt -> elt.nl  / 1000M / 1000M / 1000M / 1000M ); hidden = false |}
                {| label = "WALCL"; data = table_with_changes |> Array.map (fun elt -> elt.fed / 1000M / 1000M / 1000M / 1000M ); hidden = true |}
                {| label = "RRP";   data = table_with_changes |> Array.map (fun elt -> elt.rrp / 1000M / 1000M / 1000M / 1000M ); hidden = true |}
                {| label = "TGA";   data = table_with_changes |> Array.map (fun elt -> elt.tga / 1000M / 1000M / 1000M / 1000M ); hidden = true |}                
            |]
        |}
    |}
|}

let result_item = Json.deserialize<{| success : bool; url : string |}> (
    (new HttpClient())
        .PostAsync(
            "https://quickchart.io/chart/create", 
            new StringContent(Json.serialize item, Text.Encoding.UTF8, "application/json"))
        .Result.Content.ReadAsStringAsync().Result)

let chart_id = (new System.Uri(result_item.url)).Segments |> Array.last

cli {
    Shell PS
    Command (sprintf "Start-Process https://quickchart.io/chart-maker/view/%s" chart_id)
} |> Command.execute
