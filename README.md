# singray_Native_sdk — 4.2 development

This branch is **4.2.0-dev.1**, a development snapshot based on `dev/merge-vendor-1.0.11` (`bada120`). It is intended for integration and device testing; it is not a production release.

- Development branch: `codex/dev-4.2`
- Unity: **2022.3.62f2c1**
- Android: **ARM64**, minimum API **30**, target API **35**
- Application ID: `com.singray.SDK`; default scene: **MRTK2**
- Supplier SDK version remains **1.0.11**; the product development version is **4.2.0-dev.1**.
- Clone with Git LFS enabled (`git lfs pull`) to obtain Android AARs and local package archives.
- Unity MCP remains a Git dependency, with the resolved revision recorded in `Packages/packages-lock.json`.
- Device regression testing is required before production use. Simulation results do not validate hardware behavior.

See [4.2 development notes](Docs/development-4.2.md) for scope and validation status. The older release notes below describe previous releases.

## SDK Developer API Documentation
http://13.114.45.251/documentation/index%20-%20JP.html
## 1. Overview

**Singray AR SDK** is designed for developing AR apps on B50R, B50H, and G2 standalone devices.
The SDK project is hosted on GitHub and can be compiled and run directly after cloning.
Demos are included for MRTK2 and basic SDK capabilities.

* **For G2 devices, please use the branch:** `release_version_4.1.0`

---

## 2. What's New (release\_version\_4.1.1)

1. Added support for G2 devices
2. Fixed camera sensor (RGB/ToF) settings
3. Gesture recognition algorithm now uses CPU by default
4. Various bug fixes
5. Some known issues remain

---

## 3. Quick Start

### 1. Clone the Repository

<pre><div class="gpt-dark force-dark"><div class="code-block"><div class="markdown-body"><div class="code-block-inner custom-scrollbar custom-scrollbar-wide "><code class="hljs language-bash">git clone &lt;repo_url&gt;
</code></div></div></div></div></pre>

### 2. Unity Version Requirements

* **Recommended:** Unity 2022.3.17f or any Unity 2022.x version
* **Not supported:** Latest LTS version
* For errors on other versions, contact: [li.zhang@edge-perception.com](D:\UGit\sdk\singray_Native_sdk/mailto:li.zhang@edge-perception.com)

### 3. Import Project and Set Up SDK

* Open the Unity project in **Android** mode
* Download our Android SDK:
  [Download Link](https://drive.google.com/file/d/11d3omUPIPpGrlkPCFcey-dZQELRx-vDq/view?usp=drive_link)
* In Unity, navigate to `Edit > Project Settings > Android` and set the SDK path to the downloaded SDK

### 4. Build APK

* Build your project inside Unity to generate an APK file

### 5. Install & Test

* Install the APK on your G2 or other supported device
* Launch the app and perform tests

---

**Questions & Support:** [li.zhang@edge-perception.com](D:\UGit\sdk\singray_Native_sdk/mailto:li.zhang@edge-perception.com)

-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

-----------------------
