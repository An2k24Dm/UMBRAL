module.exports = {
  expo: {
    name: "UMBRAL",
    slug: "umbral-movil",
    scheme: "umbral",
    version: "0.1.0",
    orientation: "portrait",
    userInterfaceStyle: "automatic",
    plugins: [
      "expo-router",
      [
        "expo-camera",
        {
          cameraPermission:
            "UMBRAL necesita acceso a la cámara para escanear el código QR del tesoro.",
        },
      ],
      [
        "expo-location",
        {
          locationWhenInUsePermission:
            "UMBRAL necesita tu ubicación para mostrarla a tus compañeros de equipo durante la búsqueda del tesoro.",
        },
      ],
    ],
    android: {
      package: "com.an2k26dm.umbral",
      permissions: [
        "android.permission.CAMERA",
        "android.permission.ACCESS_FINE_LOCATION",
        "android.permission.ACCESS_COARSE_LOCATION",
      ],
    },
    extra: {
      urlApi: process.env.EXPO_PUBLIC_API_URL || "http://localhost:5000",
      eas: {
        projectId: "ac1a851e-f32b-4af4-997d-8546fa3bc626",
      },
    },
  },
};
