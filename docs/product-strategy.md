# Product strategy

Where Plumb stands against the established BIM coordination and model checking tools, what it should become, and in what order to build it.

Written October 2026 from public sources (vendor pages, release notes, reviews, job postings). Scores are an informed estimate, not a benchmark. Most vendors hide prices behind sales, so prices are indicative. Revisit this document every few months: the market moves quickly, and Revizto alone shipped an AI layer in July 2026.

## Summary

- **Plumb today** is a fast, cross-platform IFC viewer: spatial tree, properties, a `.plumb` package that reopens in about 15 ms, and a separate Unity 3D viewer. It has no clash detection, no issues and no model checking. Score: **26/100**.
- **The leaders** each own one job: Revizto owns coordination on large projects, Solibri owns rule-based checking, Navisworks owns clash detection and 4D, Dalux owns the free viewer and the construction site, BIMcollab owns open standards.
- **Plumb should not chase Revizto.** It should take a niche the leaders serve badly: small teams, macOS, a free entry point, checking and comparing models, and local AI over model data. It should work with the leaders through open standards (IFC, BCF, IDS) rather than replace them.
- **Next step:** smart filters, IDS checking, a local MCP server and a public demo are enough for a first pitch, even with the external 3D viewer.

## The market

The segment is BIM coordination, model checking and model viewing. Revizto plus the four strongest tools around it:

| Product | Vendor | Main job | Customers |
|---|---|---|---|
| Revizto | Revizto SA | Coordination and issue tracking across 2D and 3D | General contractors (about 60%), owners, large design firms |
| Navisworks + BIM Collaborate Pro | Autodesk | Clash detection, 4D/5D, cloud coordination in ACC | Contractors and designers in the Autodesk ecosystem |
| Solibri | Nemetschek | Rule-based model checking and quality assurance | BIM managers, consultants, public clients |
| BIMcollab (Zoom, Nexus, Twin) | KUBUS | Open BIM issue management (BCF), checking, document control | Designers and BIM coordinators who mix tools |
| Dalux | Dalux | Free viewer, document control, site quality and handover | Contractors and site teams, strong in Europe |

### Scores

Criteria and weights: 3D/2D and performance (15), clash detection (15), issues and collaboration (15), model checking and data quality (15), platforms (10), openness and API (10), ecosystem: 4D/5D, CDE, site (10), accessibility: price and entry threshold (10).

| Criterion | Revizto | Navisworks + BCP | Solibri | BIMcollab | Dalux | Plumb |
|---|---|---|---|---|---|---|
| 3D/2D, performance (15) | 15 | 11 | 9 | 10 | 13 | 4 |
| Clash detection (15) | 13 | 15 | 11 | 11 | 5 | 0 |
| Issues, collaboration (15) | 15 | 11 | 9 | 13 | 12 | 0 |
| Model checking (15) | 3 | 4 | 15 | 12 | 3 | 1 |
| Platforms (10) | 10 | 4 | 6 | 8 | 10 | 5 |
| Openness (10) | 7 | 5 | 9 | 10 | 6 | 5 |
| Ecosystem (10) | 6 | 10 | 7 | 7 | 10 | 1 |
| Accessibility (10) | 3 | 3 | 4 | 8 | 9 | 10 |
| **Total** | **72** | **63** | **70** | **79** | **68** | **26** |

BIMcollab tops this table because it is the most balanced, not the most powerful. The weights favour price, openness and checking, which is what matters to small teams and to a new entrant. Weighted for a large general contractor, Revizto would come first or second, next to Autodesk.

### What only they have

| Product | Unique features |
|---|---|
| Revizto | 2D sheets placed inside the 3D model. Clash automation: related clashes grouped into one issue, with rules that set assignee, priority and tags. Very large federated models on a Unity engine. QR codes on site that open an issue or a location. MCP server (July 2026) that connects ChatGPT, Claude and Copilot to live project data. Per-project licence with unlimited users. |
| Navisworks + BCP | Timeliner (4D construction sequencing). Quantification (5D takeoff). Clash Detective, the industry reference. Revit cloud co-authoring. The Autodesk ecosystem. |
| Solibri | A large library of checking rules. Custom rules in Java. Superrun: visual builder for checking workflows that run unattended. Clash matrix per discipline. Its own IDS editor. |
| BIMcollab | BCF Managers inside every authoring tool (Revit, Archicad, Tekla, Navisworks, AutoCAD), so issues are raised and closed where the model is edited. Smart Views: shareable filter-and-colour rules. Smart Issues: no duplicate clashes when several people check. Native Apple Silicon app with full IDS support. |
| Dalux | Free viewer with unlimited invitations. Mobile first, with on-site AR positioned by GPS. A complete construction suite: Box (ISO 19650 CDE), Field (QA and safety), SiteWalk (reality capture), Handover and FM. |

### Technology stacks

| Product | Stack | Why |
|---|---|---|
| Revizto | Unity (C#), about 7% of the engine used, about 7 million lines on top. Backend per job postings: PHP, Go, GraphQL, AWS, C++. | One codebase for Windows, macOS, iOS and Android; a game engine renders huge models and simplifies CAD geometry. |
| Navisworks | Native C++, Windows only; .NET plugin API. ACC web viewer: JavaScript and WebGL (APS Viewer). | A product over 20 years old from the Windows CAD world; the cloud side was built separately for the browser. |
| Solibri | Java (rule API in Java; the desktop client also runs on the JVM, unconfirmed). | Founded 1999; the JVM gave portability and Java gave rule extensibility. |
| BIMcollab Zoom | Native C/C++ with OpenGL, universal macOS build. Inferred from its dependencies (Boost, zlib, OpenSSL), not confirmed. | Speed and native Apple Silicon support (about 30% faster according to the vendor). |
| Dalux | C#/.NET, Angular/TypeScript on the web, C# Xamarin on mobile (from job postings). | One language across server, desktop and mobile. |
| Plumb | C#/.NET 10, Avalonia, xBIM, IfcOpenShell IfcConvert, SQLite; viewer on Unity 6 with glTFast. | Cross-platform desktop in one language, on open formats. |

Plumb's stack is close to the leaders': Revizto is built on Unity and Dalux on C#/.NET. The technology choice is sound; the open question is what to build with it.

## Where Plumb stands

Strengths to build on:

- **macOS.** Navisworks is Windows only, and most checking and coordination tools are Windows first. Mac users in architecture and design are poorly served.
- **Speed.** A `.plumb` package opens in about 15 ms; competitors parse IFC again or depend on a cloud conversion.
- **A database inside every package.** `model.sqlite` makes search, reports, comparison and AI access cheap to build.
- **Resilience.** The model opens even when the package cannot be saved or the geometry fails, and the app explains why.
- **Open formats end to end.** IFC in, glTF and SQLite out.

Gaps that make it look unfinished to a professional:

- 3D lives in a separate program, with no selection link back to the tree.
- No plans or sections (the Plan and Section buttons are disabled).
- No measuring, hiding, isolating or colouring by property.
- Filtering by name, type and GlobalId only, not by property values.
- No issues, no BCF, no checking.

## Positioning

What to take from each leader, and what to add:

| From | Take | Why |
|---|---|---|
| Dalux | A free, very fast viewer | Zero entry threshold; Plumb already opens packages in milliseconds |
| BIMcollab | Native Mac, open standards (BCF, IDS), Smart Views | The Mac audience is underserved, and BCF makes Plumb compatible with every other tool |
| Solibri | Rule checking, but through open IDS instead of a proprietary rule language | Quality checks without a €2,000-a-year licence |
| Revizto | 2D and 3D together, issues with viewpoints, AI access to model data | AI over project data is the topic of the moment |
| Plumb's own | Property-level version comparison by GlobalId | Navisworks (Compare) and Dalux (2D/3D compare) have it only inside expensive suites |

**The differentiator: a local MCP server over a `.plumb` package.** Revizto's MCP server is a cloud, enterprise feature. Plumb can let Claude or ChatGPT answer "how many doors on level 2 have no fire rating?" offline, with the model never leaving the machine. Firms that will not upload models to a cloud care about this.

### Pitch

> **Plumb is a fast, free IFC viewer for Mac and Windows.** It opens a model in milliseconds, shows what changed between two versions and what the model is missing (checked against IDS), and lets you ask an AI assistant about the model without sending it to the cloud. It is built on open standards (IFC, BCF, IDS), so it works alongside Revizto, Solibri and BIMcollab instead of replacing them.

Who to approach first:

1. Small architecture and interior design practices on macOS.
2. BIM consultants who receive and check models from others.
3. Teams that need model checking without enterprise licences.

## Roadmap

Each step should ship on its own and be demonstrable.

| # | Feature | Why | Size | Done when |
|---|---|---|---|---|
| 1 | Smart filters: filter and colour by property value | The first "wow" in a demo; also needed by checking and comparison | M | The tree and 3D can be filtered by any property, and a filter can be saved and shared as a file |
| 2 | IDS checking with a report | Value for BIM consultants; an open standard | M | Loading an `.ids` file lists every failing element with the requirement it breaks, and the list exports to CSV |
| 3 | Local MCP server over a package | The differentiator; SQLite is already there | S | Claude Desktop can query elements and properties of an opened `.plumb` package through a documented MCP server |
| 4 | Public demo: site, short video, signed download | Without it there is nobody to pitch to | S | A landing page with a two-minute video and macOS and Windows downloads |
| 5 | 3D inside the window, selection linked both ways | Expected from any viewer | L | Clicking an element in 3D selects it in the tree and the reverse; the separate viewer becomes optional |
| 6 | Version comparison | The main product feature | M | Two packages of the same model show added, removed and changed elements and properties, highlighted in 3D |
| 7 | BCF export of findings | Plumb results open in Revizto, BIMcollab and Solibri | S | IDS failures and comparison results export as a BCF file with viewpoints |
| 8 | Plans and sections | Closes the visible gaps in the toolbar | L | The Plan and Section modes work for every storey |

Steps 1 to 4 are enough for a first pitch. Steps 5 to 8 make Plumb a tool people keep using.

### Not now

- Clash detection: Navisworks and Revizto do it well, and it is a large geometry problem. Revisit after step 5.
- 4D/5D, CDE, site management: these belong to suites with hundreds of engineers.
- Mobile and web: desktop first, where the Mac gap is.

## Open questions

- **Licence and business model.** Open source with paid extras, freemium, or free for the portfolio? This decides who the pitch is for.
- **Name and domain.** Check that "Plumb" is free to use in this market before investing in a site.
- **Who uses Plumb today.** Five conversations with Mac-based architects or BIM consultants before step 5 will say more than any table here.

## Sources

Revizto:
- [Collaborative Clash Automation](https://revizto.com/product/collaborative-clash-automation)
- [Glossary (help center)](https://help.revizto.com/hc/en-us/articles/360002131795-Glossary)
- [Architosh: Inside Revizto (2025)](https://architosh.com/2025/08/inside-revizto-global-dominance-with-open-bim-coordination/)
- [How Revizto used a game engine](https://revizto.com/en/how-revizto-used-a-game-engine-to-unite-mining-teams-through-visualisation-tools-2)
- [Job posting with stack](https://app.welcometothejungle.com/jobs/m6abNuY1/company)
- [Pricing](https://revizto.com/pricing)
- [Revizto 5.16](https://revizto.com/en/5-16-release-update)
- [Open Data & AI Layer, MCP server](https://itwire.com/guest-articles/company-news/revizto-launches-open-data-and-ai-layer-to-give-aeco-organisations-secure-control-of-project-data-and-ai)
- [Capterra reviews](https://www.capterra.com/p/171336/Revizto/)

Autodesk:
- [BIM Collaborate vs Pro](https://autodesk.com/in/products/bim-collaborate/compare)
- [Navisworks features](https://www.autodesk.com/products/navisworks/features)
- [Autodesk on Mac (Parallels)](https://www.parallels.com/apps/autodesk/)
- [APS Viewer basics](https://aps.autodesk.com/en/docs/viewer/v7/developers_guide/viewer_basics)

Solibri:
- [Release notes 25.12.0](https://www.solibri.com/articles/solibri-25-12-0-release-notes)
- [Release notes 25.6.0](https://www.solibri.com/articles/solibri-25-6-0-release-notes)
- [Solibri Office (legacy status)](https://solibri.com/solibri-office)
- [Release notes April 2026](https://www.solibri.com/articles/solibri-release-notes-april-2026)
- [Solibri Advanced](https://www.aecplustech.com/tools/solibri-advanced)
- [Solibri Premium](https://www.aecplustech.com/tools/solibri-premium)
- [TU Wien: Solibri API rule development](https://www.tuwien.at/en/cee/ibb/zdb/research/ongoing-research-projects/bim-checking-rule-development)

BIMcollab:
- [Release history](https://www.bimcollab.com/support/releasehistory)
- [Why BIMcollab Zoom](https://www.bimcollab.com/en/products/bimcollab-zoom-campaign/)
- [Nexus trial](https://www.bimcollab.com/en/go/free-nexus-trial/)
- [Apple Silicon support](https://www.bimcollab.com/es/?p=5167)
- [Desktop Insights: BIMcollab Zoom](https://desktopinsights.com/apps/bimcollab-zoom)

Dalux:
- [Architosh: Dalux](https://architosh.com/2025/01/tooltalk-looking-at-dalux-the-worlds-fastest-bim-model-viewer)
- [AEC Magazine: Dalux](https://aecmag.com/data-management/dalux-hygge/)
- [UK G-Cloud listing](https://www.applytosupply.digitalmarketplace.service.gov.uk/g-cloud/services/248283646861949)
- [Dalux](https://www.dalux.com/)
