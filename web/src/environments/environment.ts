// Gateway ile aynı host'ta, farklı porttadır — sabit "localhost" yazmak yerine tarayıcının o an açık
// olduğu adresten (IP veya domain, hangisiyle siteye girildiyse) türetilir. Böylece bu dosya sunucuya
// taşınırken değiştirilmesi gerekmez: yerelde localhost:4300 → localhost:7500, sunucuda
// http://10.0.0.5:4300 → http://10.0.0.5:7500 olur. Gateway portu değişirse SADECE buradaki "7500"
// güncellenir (bkz. backend/docker-compose.yml GATEWAY_PORT).
export const environment = {
  production: false,
  apiBaseUrl: `${window.location.protocol}//${window.location.hostname}:7500`
};
