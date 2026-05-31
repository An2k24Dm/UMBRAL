module.exports = {
  expo: {
    name: "UMBRAL",
    slug: "umbral-movil",
    scheme: "umbral",
    version: "0.1.0",
    orientation: "portrait",
    userInterfaceStyle: "automatic",
    plugins: ["expo-router"],
    extra: {
      urlApi: process.env.EXPO_PUBLIC_API_URL || "http://localhost:5000",
    },
  },
};
