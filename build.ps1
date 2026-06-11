#!/usr/bin/env pwsh
# Convenience shim. Forwards all arguments to the Bullseye build program.
# Examples:  ./build.ps1            (default target: test)
#            ./build.ps1 pack
dotnet run --project build -- @args
