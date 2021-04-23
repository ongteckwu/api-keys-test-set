package nobodyCanQuit.service.sgis;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.io.IOException;
import java.net.URL;
import java.util.Date;

@Component
public class AccessTokenProvider {

    private static final String TOKEN_API = "https://sgisapi.kostat.go.kr/OpenAPI3/auth/authentication.json";
    private static final String CONSUMER_KEY = "9fbcd12ae4d34b8a9dd2";
    private final ObjectMapper mapper = new ObjectMapper();
    @Value("${serviceKey.SGIS}")
    private String secretKey;
    private Long accessTimeout = 0L;
    private String accessToken;

    public String getAccessToken() throws IOException {

        final Date date = new Date();
        long timer = date.getTime();

        if (accessTimeout == 0L || accessTimeout <= timer) {

            StringBuilder builder = new StringBuilder(TOKEN_API);
            builder.append("?consumer_key=").append(CONSUMER_KEY)
                    .append("&consumer_secret=").append(secretKey);

            AccessToken token = mapper.readValue(new URL(builder.toString()), AccessToken.class);
            this.accessToken = token.getResult().getAccessToken();

            accessTimeout = Long.valueOf(token.getResult().getAccessTimeout());
            accessTimeout -= (60 * 1000);

            return accessToken;
        }

        return this.accessToken;
    }
}
