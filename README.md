# 🪙 Labor-Gold Index — Jellyfin Plugin
 
**Measure a movie's real cultural impact — not in inflated dollars, but in permanent human purchasing power.**
 
The Labor-Gold Index plugin calculates inflation-adjusted box office revenue for every movie in your Jellyfin library using a median-income-to-gold purchasing power index anchored to 1920. Instead of asking "how much money did this film make," it asks: **how much real human-labor-value did audiences exchange to see it?**
 
---
 
## Why This Exists
 
Box office rankings are a lie. Not intentionally — they just measure nominal dollars, which makes every new release look like it outperforms the past. *Avatar* "beat" *Gone with the Wind*? Only if you ignore that a 1939 dollar represented fundamentally different purchasing power.
 
CPI adjustments help, but they're built on a government-managed basket of goods that gets substituted and re-weighted over time. The Labor-Gold Index takes a different approach:
 
> **G(t) = MedianIncome(t) ÷ GoldPrice(t)**
 
For any year *t*, G(t) tells you how many ounces of gold a median household's income could buy. Gold is the anchor — a millennia-spanning store of value that no central bank can print. By converting box office dollars through this ratio, you get a number that means the same thing in 1920 as it does in 2024: **how much real labor did people spend to watch this film?**
 
---
 
## Features
 
- **Automatic LG Calculation** — Fetches box office data (via OMDB and/or TMDB APIs) and calculates LG-adjusted gross, budget, profit, and ROI for every movie in your library
- **Tier Tagging** — Automatically tags movies with LG tiers (Legendary, Monumental, Epic, Blockbuster, Hit, Success, Modest) for browsing and filtering
- **Sortable Rankings** — Optionally stores the LG score in the Critic Rating field so you can sort your entire library by real cultural impact using Jellyfin's built-in sort
- **Overview Annotation** — Optionally appends a 🪙 summary line to each movie's description showing LG-adjusted gross, budget, and ROI
- **REST API** — Query the LG index for any year, calculate arbitrary conversions, and pull ranked movie lists via the plugin's API endpoints
- **Live Calculator** — Built-in calculator on the plugin config page for ad-hoc conversions
- **Scheduled Updates** — Weekly background task recalculates the entire library (configurable)
- **Historical Data 1900–2026** — Built-in gold price and median income datasets with linear interpolation for gap years
 
---
 
## LG Tier System
 
| Tier | LG-Adjusted Gross | What It Means |
|---|---|---|
| 🏆 Legendary | $3B+ | Generational cultural event |
| ⭐ Monumental | $1B – $3B | Defined an era of filmmaking |
| 🎬 Epic | $500M – $1B | Massive cultural footprint |
| 🎥 Blockbuster | $100M – $500M | Major commercial force |
| 🎞️ Hit | $50M – $100M | Significant studio success |
| ✅ Success | $10M – $50M | Solid performer |
| 📽️ Modest | < $10M | Indie / limited release |
 
---
 
## Installation
 
### Manual Install
 
1. Download the latest `Jellyfin.Plugin.LaborGoldIndex.dll` from [Releases](../../releases)
2. Place it in your Jellyfin plugin directory:
   - **Linux:** `~/.local/share/jellyfin/plugins/LaborGoldIndex/`
   - **Docker:** `/config/plugins/LaborGoldIndex/`
   - **Windows:** `%APPDATA%\jellyfin\plugins\LaborGoldIndex\`
3. Restart Jellyfin
 
### Build from Source
 
Requires [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
 
```bash
git clone https://github.com/Lodactio/labor-gold-index.git
cd labor-gold
git checkout master
dotnet publish -c Release
```
 
The built DLL will be in `bin/Release/net9.0/publish/`. Copy `Jellyfin.Plugin.LaborGoldIndex.dll` to your plugin directory and restart Jellyfin.
 
---
 
## Configuration
 
After installing, go to **Dashboard → Plugins → Labor-Gold Index** in Jellyfin.
 
### API Keys
 
| Key | Required | Source |
|---|---|---|
| **OMDB API Key** | Recommended | Free at [omdbapi.com/apikey.aspx](https://www.omdbapi.com/apikey.aspx) (1,000 req/day) |
| **TMDB API Key** | Recommended | Free at [themoviedb.org](https://www.themoviedb.org/settings/api) — provides worldwide gross + budgets |
 
At least one API key is needed for the plugin to fetch box office data. TMDB is recommended for the most complete data.
 
### Settings
 
| Setting | Default | Description |
|---|---|---|
| **Base Year** | 1920 | Anchor year for all calculations. 1920 recommended (pre-Bretton Woods distortion) |
| **Add LG Tags** | ✅ On | Adds tags like `LG:Gross:$2,066M` and `LG:Tier:Monumental` to movies |
| **Tag Prefix** | `LG` | Prefix for generated tags |
| **Use Critic Rating** | ❌ Off | Stores LG score in Critic Rating field for sortability. ⚠️ Overwrites existing critic ratings |
| **Append to Overview** | ❌ Off | Adds a 🪙 summary line to movie descriptions |
 
---
 
## API Endpoints
 
All endpoints require authentication.
 
| Endpoint | Description |
|---|---|
| `GET /LaborGold/Calculate?nominalValue=X&year=Y&baseYear=Z` | Convert any dollar amount through the LG index |
| `GET /LaborGold/Index/{year}` | Get LG index data for a specific year (income, gold price, oz buyable, era) |
| `GET /LaborGold/Index` | Full historical index table |
| `GET /LaborGold/Rankings?limit=50` | Movies in your library ranked by LG-adjusted gross |
 
### Example
 
```
GET /LaborGold/Calculate?nominalValue=2800000000&year=2019
 
{
  "NominalValue": 2800000000,
  "Year": 2019,
  "BaseYear": 1920,
  "GoldOzBuyableInYear": 49.37,
  "GoldOzBuyableInBaseYear": 67.73,
  "LaborGoldRatio": 0.729,
  "AdjustedValue": 2041200000,
  "Interpretation": "$2,800,000,000 in 2019 = $2,041,200,000 in 1920 labor-gold dollars"
}
```
 
---
 
## The Math
 
The core formula is simple:
 
```
G(t) = MedianIncome(t) / GoldPrice(t)
```
 
G(t) = how many troy ounces of gold a median household could buy in year *t*.
 
To convert a nominal dollar value:
 
```
AdjustedValue = NominalValue × ( G(releaseYear) / G(baseYear) )
```
 
When `G(t) / G(base) > 1.0`, labor bought more gold that year than the base year — a dollar of box office represented more real purchasing power. When it's `< 1.0`, it represented less.
 
### Why Gold?
 
Gold isn't magic. It's just the most stable cross-era denominator we have. It can't be printed, its supply grows at ~1.5%/year, and it's been valued by every civilization for 5,000+ years. It's the closest thing to a universal constant in economics.
 
### Why Median Income?
 
Because it represents what a *typical* person earns. GDP per capita and mean income are skewed by extreme wealth. The median tells you what the actual moviegoing public could afford.
 
---
 
## Data Sources
 
- **Gold Prices:** World Gold Council, LBMA, Kitco historical data. Pre-1971 prices reflect the official U.S. government peg ($20.67 through 1933, $35.00 through 1971).
- **Median Income:** U.S. Census Bureau, Historical Statistics of the United States, FRED. Pre-1950 figures are estimates based on BLS and NBER data.
- **Box Office:** OMDB API and/or TMDB API (user-provided keys).
 
---
 
## Compatibility
 
- **Jellyfin:** 10.11.0+
- **Framework:** .NET 9.0
- **Plugin Version:** 1.0.0
 
---
 
## License
 
See [LICENSE](LICENSE).