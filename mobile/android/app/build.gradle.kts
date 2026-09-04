import java.util.Properties

plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

// Thông báo đẩy (XI.2). Trình cắm google-services đọc google-services.json rồi sinh ra định danh dự
// án Firebase, thứ mà `Firebase.initializeApp()` gọi không tham số cần có để chạy được. Thiếu trình
// cắm này thì đặt tệp vào đúng chỗ cũng vô ích: lượt khởi tạo vẫn ném và ứng dụng rơi về "không có
// thông báo đẩy" — đúng cảnh trước ngày 04/09/2026.
//
// Áp dụng **có điều kiện**: trình cắm làm đổ cả lần dựng khi không tìm thấy tệp, mà tệp ấy chứa định
// danh dự án Firebase riêng của từng thư viện nên không đưa vào kho mã được. Không có tệp thì bản
// dựng vẫn ra bình thường, chỉ là không nhận thông báo đẩy.
if (file("google-services.json").exists()) {
    apply(plugin = "com.google.gms.google-services")
    logger.lifecycle("google-services.json: có — bản dựng này nhận được thông báo đẩy")
} else {
    logger.lifecycle("google-services.json: không có — bản dựng này không nhận thông báo đẩy")
}

// Khoá ký bản phát hành đọc từ android/key.properties (không đưa vào git — xem key.properties.example).
// Không có tệp thì ký bằng khoá debug để `flutter build apk --release` vẫn ra tệp cài thử được.
val keystoreProperties = Properties().apply {
    val file = rootProject.file("key.properties")
    if (file.exists()) file.inputStream().use { load(it) }
}
val hasReleaseKey = keystoreProperties.getProperty("storeFile") != null

android {
    namespace = "vn.bluestar.libraryconnect_mobile"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
        // flutter_local_notifications needs java.time on API 26+ devices without it in the platform
        isCoreLibraryDesugaringEnabled = true
    }

    defaultConfig {
        applicationId = "vn.bluestar.libraryconnect"
        minSdk = 26
        targetSdk = flutter.targetSdkVersion
        // Uses the version code from pubspec.yaml. When using split APKs, 1000 * ABI_VERSION
        // is added automatically by Flutter. (https://developer.android.com/studio/build/configure-apk-splits#configure-APK-versions)
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        if (hasReleaseKey) {
            create("release") {
                storeFile = rootProject.file(keystoreProperties.getProperty("storeFile"))
                storePassword = keystoreProperties.getProperty("storePassword")
                keyAlias = keystoreProperties.getProperty("keyAlias")
                keyPassword = keystoreProperties.getProperty("keyPassword")
            }
        }
    }

    buildTypes {
        release {
            signingConfig = if (hasReleaseKey) signingConfigs.getByName("release") else signingConfigs.getByName("debug")
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}

dependencies {
    coreLibraryDesugaring("com.android.tools:desugar_jdk_libs:2.1.5")
}
