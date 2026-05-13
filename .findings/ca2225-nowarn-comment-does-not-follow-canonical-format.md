# CA2225 NoWarn comment does not follow canonical format

Severity: nit
Type: code
Location: `Directory.Build.props:14`
Principle: Build gates are signal
Suppression comment mixes rule ID, description, and justification on one line instead of the structured template.

## Observation
```xml
<!-- CA2225: 'Provide a named alternate to operator op_Implicit' fights with idiomatic value-type operators where ToString()/From(string) already exist. -->
<NoWarn>$(NoWarn);CA2225</NoWarn>
```

## Why it matters
writing-csharp prescribes a specific template for `NoWarn` suppressions: `<!-- 1234 warn-description : justification -->`. The standardized shape lets reviewers and future readers scan suppressions consistently. The current form is readable but doesn't match the template, which weakens the convention before it solidifies on this branch.

## Suggested fix
```xml
<!-- CA2225 Provide a named alternate to operator op_Implicit : fights idiomatic value-type operators where ToString()/From(string) already exist -->
<NoWarn>$(NoWarn);CA2225</NoWarn>
```

A colon separates the rule description from the justification; both stay on a single comment line.
