#!/usr/bin/env sh
# Lists every automated test as a sentence, grouped by fixture. Writes docs/scenarios.md.
set -eu
cd "$(dirname "$0")/.."
out=docs/scenarios.md
{
  echo '# What Plumb is tested to do'
  echo
  echo 'Every line is the name of one automated test in `tests/Plumb.Tests`, written out as words. Regenerate with `tools/scenarios.sh`.'
  for file in tests/Plumb.Tests/*Tests.cs; do
    fixture=$(basename "$file" Tests.cs)
    echo
    echo "## $(printf '%s' "$fixture" | perl -pe 's/(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])/ /g')"
    echo
    perl -ne '
      $armed = 1 if /^\s*\[Test/;
      if ($armed && /^\s*public (?:async )?(?:void|Task) ([A-Za-z0-9_]+)\(/) {
        my $name = $1;
        $name =~ s/_/ /g;
        $name =~ s/(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])/ /g;
        $name = ucfirst(lc($name));
        print "- $name\n";
        $armed = 0;
      }
      $armed = 0 if /^\s*(private|protected|internal)/;
    ' "$file"
  done
} > "$out"
echo "wrote $out"
