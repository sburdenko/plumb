# What Plumb is tested to do

Every line is the name of one automated test in `tests/Plumb.Tests`, written out as words. Regenerate with `tools/scenarios.sh`.

## Architecture

- Core depends on no other plumb assembly
- Ifc geometry and package depend only on core
- Import assembles the pipeline from the leaf assemblies
- App talks only to import and core
- Only ifc references xbim
- Only app references avalonia
- Only package references sqlite
- Core names outcomes as states not as boolean flags
- Core values cannot change after construction

## Console Text

- Decodes utf8
- Decodes utf16 as written by ifc convert on windows
- Decodes utf16 with byte order mark
- Keeps non ascii utf8
- Empty output is empty

## Copyable Text

- Copying puts the text on the clipboard and says so for a moment
- A failed copy says so

## Element Details

- The tag is shown after the global id
- Instance groups come first and every group says where its values come from

## Element Search

- An empty query matches every element
- Text matches name class global id and tag
- Equals ignores case and the source of the value
- The property name ignores case
- Equals compares numbers as numbers
- Contains looks inside the value
- A number condition skips values that are not numbers
- Numbers are compared by the operator
- Every condition must hold
- A property no element has matches nothing
- A cancelled search stops

## Geometry Pipeline

- Every geometry node is an element of the package
- Cancel during conversion leaves no temp files
- Missing converter still loads the model

## Geometry Warning

- Words a message for every error
- Every error has a message

## Glb Info

- Counts nodes from the json chunk
- Missing or foreign files have no count

## Ifc Convert Runner

- Converts duplex with nodes named by global id
- Missing program is converter missing
- Unreadable ifc is converter failed with the reason
- Slow conversion times out and leaves no file
- Cancellation stops the converter and throws
- Timeout returns even when an orphan keeps the output open
- Error detail is the last error line without its tags

## Import Service Duplex

- Reports source file and schema
- Root is the project
- Reads all storeys ordered by elevation
- Storeys carry their elevation in metres
- Only storeys have an elevation
- Elements carry their tag
- Tree shows storeys with wall groups
- Element count matches xbim placed products plus project
- Global ids are unique
- Parents come before children
- Contained elements point to their storey
- Aggregated parts keep the storey of their whole
- Walls have common property set
- Length properties use project unit and invariant format
- Every property belongs to an element
- Properties are unique per element set and name
- Progress is monotonic and ends at hundred

## Import Service Error

- Missing file returns file not found
- Plain folder returns not ifc with a clear message
- Wrong extension returns not ifc
- Ifc extension without step header returns not ifc
- Step file without project returns parse failed
- Step file with utf8 bom passes validation
- Already cancelled token returns cancelled
- Cancel while reading returns cancelled

## Import Service Package

- Import writes package next to source
- Database element count matches xbim
- Every element index id exists in the database
- Manifest describes the source
- Opening the package gives the same model without the ifc file
- Reimport replaces the existing package
- Cancel while writing keeps the old package and leaves no draft
- Failed import leaves nothing behind
- Progress walks through every step to hundred
- Unwritable output still loads the model
- Read only source folder still loads the model
- Failed geometry still saves the package and remembers why
- Geometry starts while the model is still being read
- Geometry and manifest describe the same bytes
- Failed read stops the geometry and leaves nothing behind
- Opening a missing package returns file not found
- Opening a broken package returns package invalid
- An older package is outdated and points to its ifc file
- An older package whose ifc file is gone has no source path

## Loaded View Model

- Header and status describe the model
- Saved with geometry shows the viewer and two fine status items
- Missing geometry explains it and offers reimport
- Unsaved package marks both status items as problems
- Selecting an element marks its storey and fills the details
- A property condition narrows the tree and counts the matches
- Text and a property are combined
- Nothing matching is said plainly
- An unfinished condition keeps the tree and says what is missing
- Clearing the filter removes its status
- Clearing the filter restores the expansion the user chose

## Main Window View Model

- Starts empty without error
- Successful import shows loaded model
- A successful open is remembered
- A failed open is not remembered
- The empty state lists recent models
- An outdated package is imported again from its ifc file
- An outdated package without its ifc file explains what to do
- Ifc file is imported into its own folder
- Package is opened not imported
- Failed import shows the failure with the file and reason
- Try again opens the same file
- Opening a package says opened and importing says imported
- Cancel returns to previously loaded model
- Cancel after failure shows empty without stale error
- Commands are disabled while importing
- File dropped while picker is open is imported once
- Open package button opens the picked folder
- Picker failure shows error
- Picker cancel does not import
- Reveal shows the package in the file manager
- Unsaved package shows a warning and hides reveal
- Missing geometry shows a warning but keeps reveal
- Saved package has no warning
- Failed reveal shows the reason
- Open in3 d starts the viewer with the package
- Open in3 d is unavailable without geometry
- Open in3 d is unavailable when the package was not saved
- Missing viewer shows the reason

## Model Tree Builder

- Elements without known parent become roots
- Spatial levels are direct children
- Contained elements are grouped by type with counts
- Aggregated parts nest under their whole
- Unnamed element is labelled by type

## Model Tree Filter

- Name match keeps ancestors and updates group counts
- Match is case insensitive across branches
- Type match keeps the whole group
- Matches global id
- Matches tag
- No match returns empty
- Filtering does not mutate the source tree
- A node that matches keeps its whole subtree

## Open Target

- Opens the package when the source is unchanged
- Imports the source again when it was saved after the import
- Imports the source when the package is gone
- Opens the package when the source is gone
- Is missing when neither exists
- A model without a package opens its source

## Package Draft

- Publish replaces the old package and cleans up
- Failed publish restores the old package
- Failed restore keeps the old package on disk

## Package Reader Error

- Missing folder throws directory not found
- Missing file is a format error
- Broken manifest is a format error
- Newer format version is a format error
- An older format version is outdated and names its source
- Manifest missing field is a format error
- Manifest with null source file is a format error
- Geometry error from a newer version still opens the package
- Database that is not sqlite is a format error

## Package Round Trip

- Writes all package files
- Reads back the same elements in the same order
- Reads back the same properties
- Reads back source and schema from the manifest
- Manifest uses the documented field names
- Element index lists every element with its storey name
- Element index keeps every key even when empty
- Database has the documented tables and index
- Package without geometry file reports it missing
- Package with geometry file reports it built
- Cancelled write throws

## Presentation

- Element label drops the revit family
- Durations use milliseconds below a second
- Elevations are metres with three decimals and a true minus
- Tree rows follow the design per level
- Type group keys are stable across rebuilds
- Import steps move from pending to running to done
- The last step finishes at hundred percent
- Opening a package has one step

## Project Units

- Metres need no conversion
- Millimetres are scaled by their prefix
- Feet are scaled by their conversion factor
- A project without a length unit is taken to be in metres

## Property Extractor

- Instance value wins over type value
- Type only properties are inherited
- An instance value keeps its source when it overrides the type

## Query Parser

- A blank box is an empty query
- Plain words are one text condition
- A tag is text
- Equals compares a property value
- A tilde searches inside a value
- Numbers are compared with an operator
- An operator needs no spaces and a name may have some
- Numbers use a dot and a minus sign
- A semicolon joins conditions
- Quotes keep semicolons and operators inside a name
- A quoted text is not mistaken for a property
- An unfinished condition says what is missing

## Recent Model File Store

- Saved models load back
- Saving creates the folder and leaves no temporary file
- No file means no models
- A corrupt file means no models and is logged
- An unknown version means no models
- Entries without an absolute path are skipped

## Recent Model List

- Newest opened comes first
- Opening again moves the model up without a duplicate
- An ifc file and its package are one model
- Pinned models stay above newer ones
- Opening a pinned model keeps it pinned
- Unpinning puts the model back in time order
- Remove drops only that model
- Only the newest unpinned models are kept
- Of sorts and drops duplicates keeping the newest

## Recent Models View Model

- Starts empty without models
- A loaded model is remembered and saved
- Pinned models have their own section
- Pinning moves the model and is saved
- Shows the ten newest until asked for all
- The filter matches names and folders and shows every match
- The filter appears only for a long list
- Menus list pinned models and the ten newest whatever the filter
- Opening a model opens its package
- A missing model cannot be opened but can be removed
- Reveal shows the file that would open
- Rows describe the model

## Relative Time

- Describes how long ago
- Shows the year for an earlier year
- Counts days in the viewers time zone

## Screen View Model

- The window shows empty importing failed or loaded

## Viewer Launcher

- Missing location has no viewer
- Executable path is used as is
- Finds the binary inside the app bundle
- Finds the linux player
- Missing package is reported before starting the viewer
- Finds the windows executable
