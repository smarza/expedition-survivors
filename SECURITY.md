# Security policy

## Unity runtime advisory CVE-2025-59489

Do not distribute a build produced by an affected Unity Editor. The historical 0.7.1 baseline was authored with Unity `6000.0.40f1`, which predates Unity's runtime fix.

Development from 0.8.0 onward must use Unity `6000.5.4f1` or a newer supported patched release. The first patched release in the Unity 6.0 LTS line is `6000.0.58f2`.

All Windows, Linux, macOS and Android deliverables must be rebuilt and smoke-tested with the patched Editor. Previously produced prototype executables are test artifacts only and must not be distributed as release builds.

Official advisory: https://unity.com/security/sept-2025-01

## Reporting

Do not open a public issue for a suspected vulnerability. Contact the repository owner privately with reproduction details, affected version and platform.

