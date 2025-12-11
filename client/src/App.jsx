import { useEffect, useState } from "react";

const API_BASE =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, "") ||
  "http://localhost:5000";

function App() {
  const [file, setFile] = useState(null);
  const [status, setStatus] = useState("");
  const [videos, setVideos] = useState([]);
  const [isUploading, setIsUploading] = useState(false);

  const fetchVideos = async () => {
    try {
      const res = await fetch(`${API_BASE}/api/videos`);
      if (!res.ok) throw new Error("List failed");
      const data = await res.json();
      setVideos(data);
    } catch (err) {
      console.error(err);
    }
  };

  useEffect(() => {
    fetchVideos();
    const interval = setInterval(fetchVideos, 5000);
    return () => clearInterval(interval);
  }, []);

  const handleUpload = async () => {
    if (!file) {
      setStatus("Lütfen bir dosya seçin.");
      return;
    }

    setIsUploading(true);
    setStatus("Pre-signed URL alınıyor...");

    try {
      const initRes = await fetch(`${API_BASE}/api/upload/init`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fileName: file.name }),
      });

      if (!initRes.ok) throw new Error("Pre-signed URL alınamadı.");
      const { uploadUrl, s3Key } = await initRes.json();

      setStatus("S3'e yükleniyor...");
      const uploadRes = await fetch(uploadUrl, {
        method: "PUT",
        body: file,
        headers: { "Content-Type": "application/octet-stream" },
      });
      if (!uploadRes.ok) throw new Error("S3 yüklemesi başarısız.");

      setStatus("Backend'e bildirim gönderiliyor...");
      const completeRes = await fetch(`${API_BASE}/api/upload/complete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fileName: file.name, s3Key }),
      });

      if (!completeRes.ok) throw new Error("Backend bildirimi başarısız.");

      setStatus("Yükleme tamamlandı. İşleme alındı.");
      setFile(null);
      await fetchVideos();
    } catch (err) {
      console.error(err);
      setStatus(err.message || "Bir hata oluştu.");
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 text-gray-800">
      <div className="max-w-5xl mx-auto px-4 py-10">
        <header className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold">CloudConverter</h1>
            <p className="text-sm text-gray-500">
              Video yükle, otomatik işleme ve thumbnail üretimi
            </p>
          </div>
          <span className="px-3 py-1 rounded-full bg-blue-100 text-blue-700 text-sm">
            SaaS Demo
          </span>
        </header>

        <div className="bg-white shadow-sm rounded-lg p-6 mb-8 border border-gray-100">
          <h2 className="text-lg font-semibold mb-4">Video Yükle</h2>
          <div className="flex items-center gap-3">
            <input
              type="file"
              accept="video/*"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            />
            <button
              onClick={handleUpload}
              disabled={isUploading}
              className="px-4 py-2 rounded bg-blue-600 text-white font-medium hover:bg-blue-700 disabled:opacity-50"
            >
              {isUploading ? "Yükleniyor..." : "Yükle"}
            </button>
          </div>
          {status && <p className="mt-3 text-sm text-gray-600">{status}</p>}
        </div>

        <div className="bg-white shadow-sm rounded-lg p-6 border border-gray-100">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold">Videolar</h2>
            <button
              onClick={fetchVideos}
              className="text-sm text-blue-600 hover:underline"
            >
              Yenile
            </button>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b">
                  <th className="py-2 pr-4">ID</th>
                  <th className="py-2 pr-4">Dosya</th>
                  <th className="py-2 pr-4">Durum</th>
                  <th className="py-2 pr-4">Thumbnail</th>
                  <th className="py-2 pr-4">Oluşturma</th>
                </tr>
              </thead>
              <tbody>
                {videos.map((v) => (
                  <tr key={v.id} className="border-b last:border-0">
                    <td className="py-2 pr-4">{v.id}</td>
                    <td className="py-2 pr-4">{v.fileName}</td>
                    <td className="py-2 pr-4">
                      <span
                        className={`px-2 py-1 rounded text-xs ${
                          v.status === "Completed"
                            ? "bg-green-100 text-green-700"
                            : "bg-yellow-100 text-yellow-700"
                        }`}
                      >
                        {v.status}
                      </span>
                    </td>
                    <td className="py-2 pr-4">
                      {v.thumbnailUrl ? (
                        <a
                          href={v.thumbnailUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="text-blue-600 hover:underline"
                        >
                          Görüntüle
                        </a>
                      ) : (
                        "-"
                      )}
                    </td>
                    <td className="py-2 pr-4">
                      {new Date(v.createdAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
                {videos.length === 0 && (
                  <tr>
                    <td className="py-3 text-gray-500" colSpan="5">
                      Henüz video yok.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;

