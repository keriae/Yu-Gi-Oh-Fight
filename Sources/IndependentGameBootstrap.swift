// Original launcher contributions copyright (c) 2026 keriae. See COPYRIGHT.md.
import AppKit
import CryptoKit
import Darwin
import Foundation

// Directly runs the native Unity client; no external original game, Steam, or Wine.
// Game resources remain inside this bundle, while writable state lives outside it.
@main
struct IndependentGameBootstrap {
    struct State: Codable {
        var version = 2
        var source: String
        var seeds: [String: String] = [:]
        var links: [String: String] = [:]
    }

    static let writablePrefixes = ["Picture/Art2", "Picture/DIY", "Picture/MyCardAvatars", "Picture/CardGenerated",
                                   "Video/Art", "StandaloneOSX/MonsterCutin", "StandaloneOSX/MonsterCutin2"]
    static let folderName = Bundle.main.object(forInfoDictionaryKey: "MDPro3StateFolder") as? String
        ?? "Yu-Gi-Oh! Fight"
    static let applicationIdentifier = Bundle.main.bundleIdentifier ?? "io.github.keriae.yugiohfight"
    static let applicationName = Bundle.main.object(forInfoDictionaryKey: "CFBundleName") as? String
        ?? "Yu-Gi-Oh! Fight"
    enum InterfaceLanguage: Sendable {
        case simplifiedChinese, traditionalChinese, english

        static func from(_ identifier: String) -> InterfaceLanguage {
            let code = identifier.replacingOccurrences(of: "_", with: "-").lowercased()
            guard code.hasPrefix("zh") else { return .english }
            return code.contains("hant") || code.hasPrefix("zh-tw") || code.hasPrefix("zh-hk") || code.hasPrefix("zh-mo")
                ? .traditionalChinese : .simplifiedChinese
        }
    }
    // Resolve the saved interface choice once, before background preparation.
    // The immutable, Sendable value is safe to use from both AppKit and workers.
    static let interfaceLanguage: InterfaceLanguage = {
        if let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first {
            let configURL = support.appendingPathComponent(folderName).appendingPathComponent("Data/config.conf")
            if let config = try? String(contentsOf: configURL, encoding: .utf8) {
                for line in config.components(separatedBy: .newlines) {
                    let pair = line.components(separatedBy: "->")
                    if pair.count == 2 && pair[0].trimmingCharacters(in: .whitespaces) == "Language" {
                        let saved = pair[1].trimmingCharacters(in: .whitespaces)
                        if !saved.isEmpty { return InterfaceLanguage.from(saved) }
                    }
                }
            }
        }
        return InterfaceLanguage.from(Locale.preferredLanguages.first ?? "en-US")
    }()
    static func localized(_ simplified: String, _ english: String, _ traditional: String) -> String {
        switch interfaceLanguage {
        case .simplifiedChinese: return simplified
        case .traditionalChinese: return traditional
        case .english: return english
        }
    }
    static let policy = """
    (version 1)
    (allow default)
    (deny network*)
    (allow network-outbound)
    (allow network-outbound (remote ip "localhost:*"))
    (allow network-inbound (local ip "localhost:*"))
    (allow network-bind (local ip "localhost:*"))
    """

    @MainActor final class PreparationWindow {
        let window: NSWindow
        let status = NSTextField(labelWithString: IndependentGameBootstrap.localized(
            "正在准备本地游戏资源…", "Preparing local game resources…", "正在準備本機遊戲資源…"))
        let indicator = NSProgressIndicator()

        init() {
            window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 460, height: 190),
                              styleMask: [.titled], backing: .buffered, defer: false)
            window.title = IndependentGameBootstrap.applicationName
            window.isReleasedWhenClosed = false
            let heading = NSTextField(labelWithString: IndependentGameBootstrap.localized(
                "游戏即将开始", "Your duel is almost ready", "遊戲即將開始"))
            heading.font = .systemFont(ofSize: 20, weight: .semibold)
            status.font = .systemFont(ofSize: 13)
            let detail = NSTextField(labelWithString: IndependentGameBootstrap.localized(
                "首次启动或更新后的准备时间可能较长。", "First launch or an update may take a little longer.", "首次啟動或更新後的準備時間可能較長。"))
            detail.textColor = .secondaryLabelColor
            detail.font = .systemFont(ofSize: 12)
            indicator.style = .bar
            indicator.isIndeterminate = true
            indicator.usesThreadedAnimation = true
            let stack = NSStackView(views: [heading, status, indicator, detail])
            stack.orientation = .vertical
            stack.alignment = .leading
            stack.spacing = 15
            stack.translatesAutoresizingMaskIntoConstraints = false
            window.contentView!.addSubview(stack)
            NSLayoutConstraint.activate([
                stack.leadingAnchor.constraint(equalTo: window.contentView!.leadingAnchor, constant: 28),
                stack.trailingAnchor.constraint(equalTo: window.contentView!.trailingAnchor, constant: -28),
                stack.centerYAnchor.constraint(equalTo: window.contentView!.centerYAnchor),
                indicator.widthAnchor.constraint(equalTo: stack.widthAnchor)
            ])
        }

        func show() {
            window.center()
            window.makeKeyAndOrderFront(nil)
            indicator.startAnimation(nil)
            NSApplication.shared.activate(ignoringOtherApps: true)
        }

        func update(_ message: String) { status.stringValue = message }

        func close() {
            indicator.stopAnimation(nil)
            window.close()
        }
    }

    @MainActor static func main() {
        do {
            guard let resources = Bundle.main.resourceURL else {
                throw failure("应用资源目录缺失。", "The application resources folder is missing.", "應用程式資源目錄遺失。")
            }
            let client = resources.appendingPathComponent("Client", isDirectory: true).resolvingSymlinksInPath()
            let executable = client.appendingPathComponent("MDPro3.app/Contents/MacOS/MDPro3")
            let files = FileManager.default
            guard files.isExecutableFile(atPath: executable.path) else {
                throw failure("游戏客户端不完整，请安装完整的应用后重试。", "The game client is incomplete. Install a complete app and try again.", "遊戲用戶端不完整，請安裝完整的應用程式後重試。")
            }
            guard files.isExecutableFile(atPath: "/usr/bin/sandbox-exec") else {
                throw failure("这台 Mac 不支持当前单机网络隔离方式，未启动客户端。", "This Mac does not support the offline network isolation method. The game was not started.", "這台 Mac 不支援目前的單機網路隔離方式，未啟動用戶端。")
            }
            let support = try files.url(for: .applicationSupportDirectory, in: .userDomainMask,
                                        appropriateFor: nil, create: true)
                .appendingPathComponent(folderName, isDirectory: true)
            try files.createDirectory(at: support, withIntermediateDirectories: true)
            let lock = try acquireLaunchLock(in: support)
            guard lock >= 0 else { return }
            defer { releaseLaunchLock(lock, in: support) }
            // Register this process before resource preparation so another
            // launch activates the preparation window rather than an old PID.
            try recordLaunchPID(in: support)
            let application = NSApplication.shared
            application.setActivationPolicy(.regular)
            let preparation = PreparationWindow()
            preparation.show()
            // A full resource upgrade can change tens of thousands of links.
            // Keep AppKit responsive while the same process retains its flock.
            DispatchQueue.global(qos: .userInitiated).async {
                do {
                    try prepareState(source: client, destination: support) { message in
                        DispatchQueue.main.async { preparation.update(message) }
                    }
                    DispatchQueue.main.async {
                        preparation.close()
                        do {
                            try launchPreparedGame(client: client, executable: executable, support: support, lock: lock)
                        } catch {
                            releaseLaunchLock(lock, in: support)
                            showFailure(error)
                        }
                    }
                } catch {
                    DispatchQueue.main.async {
                        preparation.close()
                        releaseLaunchLock(lock, in: support)
                        showFailure(error)
                    }
                }
            }
            application.run()
        } catch {
            showFailure(error)
        }
    }

    @MainActor static func showFailure(_ error: Error) -> Never {
        let application = NSApplication.shared
        application.setActivationPolicy(.regular)
        application.activate(ignoringOtherApps: true)
        let alert = NSAlert()
        alert.messageText = localized("游戏未能启动", "The game could not start", "遊戲未能啟動")
        let detail = error as NSError
        alert.informativeText = detail.domain == "MasterDuelIndependent" ? detail.localizedDescription
            : localized("资源准备或启动失败，请检查数据目录权限与可用空间后重试。", "Resource preparation or launch failed. Check data-folder permissions and available storage, then try again.", "資源準備或啟動失敗，請檢查資料目錄權限與可用空間後重試。")
                + "\n\n" + detail.domain + " (\(detail.code))"
        alert.addButton(withTitle: localized("关闭", "Close", "關閉"))
        alert.runModal()
        exit(1)
    }

    @MainActor static func launchPreparedGame(client: URL, executable: URL, support: URL, lock: Int32) throws -> Never {
        let files = FileManager.default
        let logs = support.appendingPathComponent("Logs", isDirectory: true)
        try files.createDirectory(at: logs, withIntermediateDirectories: true)
        let logURL = logs.appendingPathComponent("game.log")
        if !files.fileExists(atPath: logURL.path) { _ = files.createFile(atPath: logURL.path, contents: nil) }
        let log = try FileHandle(forWritingTo: logURL)
        defer { try? log.close() }
        try log.seekToEnd()
        try log.write(contentsOf: Data("\n[Independent native game] \(ISO8601DateFormatter().string(from: Date()))\n".utf8))
        let escapedSource = client.path.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
        let resourceProtection = "\n(deny file-write* (subpath \"\(escapedSource)\"))"
        let arguments = ["/usr/bin/sandbox-exec", "-p", policy + resourceProtection, executable.path]
            + initialWindowArguments(in: support)
        try replaceProcess(executable: "/usr/bin/sandbox-exec", arguments: arguments,
                           environment: nativeEnvironment(in: support, inherited: ProcessInfo.processInfo.environment),
                           workingDirectory: support, logDescriptor: log.fileDescriptor, launchLock: lock)
    }

    static func failure(_ simplified: String, _ english: String, _ traditional: String) -> NSError {
        NSError(domain: "MasterDuelIndependent", code: 1, userInfo: [NSLocalizedDescriptionKey: localized(simplified, english, traditional)])
    }

    static func nativeEnvironment(in support: URL, inherited: [String: String]) -> [String: String] {
        var environment = inherited
        environment["MDPRO3_STATE_ROOT"] = support.path
        // LaunchServices injected these for the launcher. Removing its service
        // identity lets the replacement Unity executable identify its own bundle.
        environment.removeValue(forKey: "__CFBundleIdentifier")
        environment.removeValue(forKey: "XPC_SERVICE_NAME")
        return environment
    }

    static func cStrings(_ values: [String]) throws -> [UnsafeMutablePointer<CChar>?] {
        var pointers: [UnsafeMutablePointer<CChar>?] = []
        for value in values {
            guard let pointer = strdup(value) else {
                for pointer in pointers { free(pointer) }
                throw failure("无法分配游戏启动参数。", "Could not allocate the game launch arguments.", "無法配置遊戲啟動參數。")
            }
            pointers.append(pointer)
        }
        pointers.append(nil)
        return pointers
    }

    // Keep the PID that LaunchServices registered for this application. The
    // sandbox and Unity replace this launcher instead of creating a second app.
    // The inherited flock descriptor remains open through both exec calls and
    // the kernel releases it when the game exits, including a crash or signal.
    static func replaceProcess(executable: String, arguments: [String], environment: [String: String],
                               workingDirectory: URL, logDescriptor: Int32, launchLock: Int32) throws -> Never {
        guard chdir(workingDirectory.path) == 0 else { throw failure("无法打开游戏数据目录：\(errno)", "Could not open the game data folder: \(errno)", "無法開啟遊戲資料目錄：\(errno)") }
        var argv = try cStrings(arguments)
        defer { for pointer in argv { free(pointer) } }
        var envp = try cStrings(environment.sorted { $0.key < $1.key }.map { $0.key + "=" + $0.value })
        defer { for pointer in envp { free(pointer) } }
        let flags = fcntl(launchLock, F_GETFD)
        guard flags >= 0, fcntl(launchLock, F_SETFD, flags & ~FD_CLOEXEC) == 0 else {
            throw failure("无法保留游戏启动锁：\(errno)", "Could not retain the game launch lock: \(errno)", "無法保留遊戲啟動鎖：\(errno)")
        }
        guard dup2(logDescriptor, STDOUT_FILENO) >= 0, dup2(logDescriptor, STDERR_FILENO) >= 0 else {
            throw failure("无法记录游戏日志：\(errno)", "Could not write the game log: \(errno)", "無法記錄遊戲日誌：\(errno)")
        }
        try recordLaunchPID(in: workingDirectory)
        // This defer runs only when exec fails. After a normal game exit the
        // marker may remain; callers determine liveness from flock, then use the
        // PID only for activation. The next successful launch overwrites it.
        defer { clearLaunchPID(in: workingDirectory) }
        let error = executable.withCString { executablePointer -> Int32 in
            argv.withUnsafeMutableBufferPointer { argumentPointers in
                envp.withUnsafeMutableBufferPointer { environmentPointers in
                    _ = execve(executablePointer, argumentPointers.baseAddress!, environmentPointers.baseAddress!)
                    return errno
                }
            }
        }
        let reason = String(cString: strerror(error))
        throw failure("无法启动游戏：\(reason)（\(error)）", "Could not start the game: \(reason) (\(error))", "無法啟動遊戲：\(reason)（\(error)）")
    }

    // Unity persists desktop dimensions in its own preferences. Supply a usable
    // first window only when neither Unity nor the game has a saved choice.
    static func initialWindowArguments(in support: URL) -> [String] {
        let config = (try? String(contentsOf: support.appendingPathComponent("Data/config.conf"), encoding: .utf8)) ?? ""
        let hasConfigPreference = config.components(separatedBy: .newlines).contains { line in
            let key = line.components(separatedBy: "->").first
            return key == "ScreenMode" || key == "Resolution"
        }
        let domain = applicationIdentifier as CFString
        let keys = ["Screenmanager Fullscreen mode", "Screenmanager Resolution Width", "Screenmanager Resolution Height"]
        let hasUnityPreference = keys.contains { CFPreferencesCopyAppValue($0 as CFString, domain) != nil }
        guard !hasConfigPreference, !hasUnityPreference else { return [] }
        return ["-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "800"]
    }

    static func recordLaunchPID(in support: URL) throws {
        try Data(String(getpid()).utf8).write(to: support.appendingPathComponent(".native-game-pid"), options: .atomic)
    }

    static func clearLaunchPID(in support: URL) {
        let marker = support.appendingPathComponent(".native-game-pid")
        guard (try? String(contentsOf: marker, encoding: .utf8)) == String(getpid()) else { return }
        try? FileManager.default.removeItem(at: marker)
    }

    static func releaseLaunchLock(_ descriptor: Int32, in support: URL) {
        // Clear our marker while the lock still excludes another launch.
        clearLaunchPID(in: support)
        _ = close(descriptor)
    }

    // The game retains this lock until it exits. A second launch raises the
    // existing game instead of racing migration or starting another local server.
    static func acquireLaunchLock(in support: URL) throws -> Int32 {
        let fd = open(support.appendingPathComponent(".native-game-lock").path, O_CREAT | O_RDWR | O_CLOEXEC, 0o600)
        guard fd >= 0 else { throw failure("无法锁定游戏数据目录。", "Could not lock the game data folder.", "無法鎖定遊戲資料目錄。") }
        if flock(fd, LOCK_EX | LOCK_NB) == 0 { return fd }
        let error = errno
        _ = close(fd)
        guard error == EWOULDBLOCK else { throw failure("无法锁定游戏数据目录：\(error)", "Could not lock the game data folder: \(error)", "無法鎖定遊戲資料目錄：\(error)") }
        if let value = try? String(contentsOf: support.appendingPathComponent(".native-game-pid"), encoding: .utf8),
           let pid = Int32(value), let game = NSRunningApplication(processIdentifier: pid) {
            game.activate(options: [.activateAllWindows])
        }
        return -1
    }

    static func digest(_ file: URL) throws -> String {
        let input = try FileHandle(forReadingFrom: file)
        defer { try? input.close() }
        var hash = SHA256()
        while let data = try input.read(upToCount: 1024 * 1024), !data.isEmpty { hash.update(data: data) }
        return hash.finalize().map { String(format: "%02x", $0) }.joined()
    }

    static func copyAtomically(_ source: URL, to target: URL) throws {
        let files = FileManager.default
        let temporary = target.deletingLastPathComponent().appendingPathComponent(".bootstrap-" + UUID().uuidString)
        defer { try? files.removeItem(at: temporary) }
        try files.copyItem(at: source, to: temporary)
        guard rename(temporary.path, target.path) == 0 else {
            throw failure("无法更新游戏数据：\(target.path)（\(errno)）", "Could not update game data: \(target.path) (\(errno))", "無法更新遊戲資料：\(target.path)（\(errno)）")
        }
    }

    static func relativePath(of file: URL, in folder: URL) throws -> String {
        let base = folder.resolvingSymlinksInPath().pathComponents
        let components = file.resolvingSymlinksInPath().pathComponents
        guard components.starts(with: base), components.count > base.count else {
            throw failure("资源路径超出目录：\(file.path)", "Resource path is outside its folder: \(file.path)", "資源路徑超出目錄：\(file.path)")
        }
        return components.dropFirst(base.count).joined(separator: "/")
    }

    static func prepareState(source: URL, destination: URL, progress: (@Sendable (String) -> Void)? = nil) throws {
        let source = source.standardizedFileURL.resolvingSymlinksInPath()
        let files = FileManager.default
        try files.createDirectory(at: destination, withIntermediateDirectories: true)
        let marker = destination.appendingPathComponent(".native-game-bootstrap.json")
        let previousData = try? Data(contentsOf: marker)
        var previous = previousData.flatMap { try? JSONDecoder().decode(State.self, from: $0) }
        if previous == nil, let data = previousData,
           let legacy = try? JSONSerialization.jsonObject(with: data) as? [String: String],
           let oldSource = legacy["source"] {
            previous = State(source: oldSource)
        }
        var next = State(source: source.path)
        var migratedWritableDirectories = Set<String>()
        // Upgrade unchanged shipped databases/scripts/deck examples. A user's
        // changed copy or newly added file stays in the writable data tree.
        for name in ["Data", "Deck", "Puzzle", "Replay"] {
            progress?(name == "Data" ? localized("正在准备卡片数据…", "Preparing card data…", "正在準備卡片資料…")
                : localized("正在准备卡组与练习…", "Preparing decks and practice…", "正在準備牌組與練習…"))
            let original = source.appendingPathComponent(name, isDirectory: true)
            let writable = destination.appendingPathComponent(name, isDirectory: true)
            if name == "Replay" && !files.fileExists(atPath: original.path) { continue }
            guard files.fileExists(atPath: original.path) else { throw failure("缺少游戏数据目录：\(name)", "Game data folder is missing: \(name)", "缺少遊戲資料目錄：\(name)") }
            if try ensureDirectory(writable, logical: name, source: source, previous: previous) {
                migratedWritableDirectories.insert(name)
            }
            guard let entries = files.enumerator(at: original, includingPropertiesForKeys: [.isDirectoryKey, .isRegularFileKey]) else {
                throw failure("无法读取游戏数据目录：\(original.path)", "Could not read the game data folder: \(original.path)", "無法讀取遊戲資料目錄：\(original.path)")
            }
            for case let file as URL in entries {
                let relative = try relativePath(of: file, in: original)
                let logical = name + "/" + relative
                let target = writable.appendingPathComponent(relative)
                let attributes = try file.resourceValues(forKeys: [.isDirectoryKey, .isRegularFileKey])
                if attributes.isDirectory == true {
                    if try ensureDirectory(target, logical: logical, source: source, previous: previous) {
                        migratedWritableDirectories.insert(logical)
                    }
                    continue
                }
                guard attributes.isRegularFile == true else { continue }
                if (try? files.destinationOfSymbolicLink(atPath: target.path)) != nil {
                    throw failure("游戏数据文件必须可写，不能是外部链接：\(target.path)", "Game data must be writable files rather than external links: \(target.path)", "遊戲資料檔案必須可寫入，不能是外部連結：\(target.path)")
                }
                let incoming = try digest(file)
                if !files.fileExists(atPath: target.path) {
                    try copyAtomically(file, to: target)
                    next.seeds[logical] = incoming
                } else {
                    let current = try digest(target)
                    let oldFile = previous.map { URL(fileURLWithPath: $0.source).appendingPathComponent(logical) }
                    // A legacy directory link allowed writes into the old source.
                    // Its current bytes cannot identify an untouched shipped seed.
                    let migrated = migratedWritableDirectories.contains { logical.hasPrefix($0 + "/") }
                    let oldDigest = previous?.seeds[logical] ?? (migrated ? nil : oldFile.flatMap { try? digest($0) })
                    if current == incoming || current == oldDigest {
                        if current != incoming { try copyAtomically(file, to: target) }
                        next.seeds[logical] = incoming
                    } else if let tracked = previous?.seeds[logical] {
                        next.seeds[logical] = tracked
                    }
                }
            }
        }
        for name in ["Replay", "TempFolder"] {
            try ensureDirectory(destination.appendingPathComponent(name), logical: name, source: source, previous: previous)
        }
        for name in ["StandaloneOSX", "Picture", "Video"] {
            progress?(name == "StandaloneOSX" ? localized("正在准备游戏场景…", "Preparing game scenes…", "正在準備遊戲場景…")
                : localized("正在准备卡片画面…", "Preparing card visuals…", "正在準備卡片畫面…"))
            let resource = source.appendingPathComponent(name, isDirectory: true)
            let link = destination.appendingPathComponent(name)
            // Videos are optional in the baseline; Unity creates Video/Art.
            guard name == "Video" || files.fileExists(atPath: resource.path) else { throw failure("缺少画面资源目录：\(name)", "Visual resource folder is missing: \(name)", "缺少畫面資源目錄：\(name)") }
            try ensureDirectory(link, logical: name, source: source, previous: previous)
            if files.fileExists(atPath: resource.path) {
                try mapResourceTree(source: resource, destination: link, logicalRoot: name, previous: previous, next: &next, progress: progress)
            }
            for path in writablePrefixes where path.hasPrefix(name + "/") {
                let relative = String(path.dropFirst(name.count + 1))
                try ensureDirectory(link.appendingPathComponent(relative), logical: path, source: source, previous: previous)
            }
        }
        // Remove only links recorded by this bootstrap when a new distribution
        // drops an asset. Real files, imports and unrelated links are preserved.
        for (logical, oldTarget) in previous?.links ?? [:] where next.links[logical] == nil {
            let path = destination.appendingPathComponent(logical)
            if (try? files.destinationOfSymbolicLink(atPath: path.path)) == oldTarget { try files.removeItem(at: path) }
        }
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        try encoder.encode(next).write(to: marker, options: .atomic)
        progress?(localized("准备完成，正在启动游戏…", "Ready. Starting the game…", "準備完成，正在啟動遊戲…"))
    }

    @discardableResult
    static func ensureDirectory(_ path: URL, logical: String, source: URL, previous: State?) throws -> Bool {
        let files = FileManager.default
        var preservedWholeDirectory = false
        if let target = try? files.destinationOfSymbolicLink(atPath: path.path) {
            let current = source.appendingPathComponent(logical).path
            let old = previous.map { URL(fileURLWithPath: $0.source).appendingPathComponent(logical).path }
            guard target == current || target == old else { throw failure("目录链接来源不匹配：\(path.path)", "Folder link source does not match: \(path.path)", "目錄連結來源不符：\(path.path)") }
            // Writable data can include user-only files absent from the new
            // distribution. Preserve the whole tree before replacing its link.
            let snapshot = path.deletingLastPathComponent().appendingPathComponent(".bootstrap-migrate-" + UUID().uuidString)
            defer { try? files.removeItem(at: snapshot) }
            let writableTree = ["Data", "Deck", "Puzzle", "Replay", "TempFolder"].contains {
                logical == $0 || logical.hasPrefix($0 + "/")
            } || writablePrefixes.contains { logical == $0 || logical.hasPrefix($0 + "/") }
            if writableTree, files.fileExists(atPath: target) {
                try files.copyItem(at: URL(fileURLWithPath: target), to: snapshot)
                preservedWholeDirectory = true
            } else {
                // Read-only resource roots keep only their writable descendants.
                for prefix in writablePrefixes where prefix.hasPrefix(logical + "/") {
                    let relative = String(prefix.dropFirst(logical.count + 1))
                    let oldFolder = URL(fileURLWithPath: target).appendingPathComponent(relative)
                    if files.fileExists(atPath: oldFolder.path) {
                        let copy = snapshot.appendingPathComponent(relative)
                        try files.createDirectory(at: copy.deletingLastPathComponent(), withIntermediateDirectories: true)
                        try files.copyItem(at: oldFolder, to: copy)
                    }
                }
            }
            try files.removeItem(at: path)
            if files.fileExists(atPath: snapshot.path) {
                do { try files.moveItem(at: snapshot, to: path) }
                catch {
                    try files.createSymbolicLink(atPath: path.path, withDestinationPath: target)
                    throw error
                }
            }
        }
        try files.createDirectory(at: path, withIntermediateDirectories: true)
        return preservedWholeDirectory
    }

    static func mapResourceTree(source: URL, destination: URL, logicalRoot: String, previous: State?, next: inout State,
                                progress: (@Sendable (String) -> Void)? = nil) throws {
        let files = FileManager.default
        guard let enumeration = files.enumerator(at: source, includingPropertiesForKeys: [.isDirectoryKey, .isRegularFileKey], options: []) else {
            throw failure("无法读取资源目录：\(source.path)", "Could not read the resource folder: \(source.path)", "無法讀取資源目錄：\(source.path)")
        }
        var processed = 0
        for case let original as URL in enumeration {
            processed += 1
            if processed % 256 == 0 {
                let phase = logicalRoot == "StandaloneOSX" ? localized("游戏场景", "game scenes", "遊戲場景")
                    : localized("卡片画面", "card visuals", "卡片畫面")
                progress?(localized("正在准备\(phase)（\(processed) 项）…", "Preparing \(phase) (\(processed) items)…", "正在準備\(phase)（\(processed) 項）…"))
            }
            // Background threads have no AppKit event-loop autorelease pool.
            // Release temporary Foundation path objects for each mapped entry.
            try autoreleasepool {
                let relative = try relativePath(of: original, in: source)
                let logical = logicalRoot + "/" + relative
                let target = destination.appendingPathComponent(relative)
                let attributes = try original.resourceValues(forKeys: [.isDirectoryKey, .isRegularFileKey])
                if attributes.isDirectory == true {
                    try ensureDirectory(target, logical: logical, source: source.deletingLastPathComponent(), previous: previous)
                    return
                }
                guard attributes.isRegularFile == true else { return }
                let mutable = writablePrefixes.contains { logical == $0 || logical.hasPrefix($0 + "/") }
                if let existing = try? files.destinationOfSymbolicLink(atPath: target.path) {
                    if !mutable && existing == original.path { next.links[logical] = existing; return }
                    let old = previous?.links[logical] ?? previous.map { URL(fileURLWithPath: $0.source).appendingPathComponent(logical).path }
                    guard existing == original.path || existing == old else {
                        throw failure("资源链接来源不匹配：\(target.path)", "Resource link source does not match: \(target.path)", "資源連結來源不符：\(target.path)")
                    }
                    if mutable, files.fileExists(atPath: target.path) {
                        try copyAtomically(URL(fileURLWithPath: existing), to: target)
                        return
                    }
                    try files.removeItem(at: target)
                } else if files.fileExists(atPath: target.path) {
                    return
                }
                try files.createDirectory(at: target.deletingLastPathComponent(), withIntermediateDirectories: true)
                if mutable { try files.copyItem(at: original, to: target) }
                else {
                    try files.createSymbolicLink(atPath: target.path, withDestinationPath: original.path)
                    next.links[logical] = original.path
                }
            }
        }
    }
}
