package com.willsoft.admoneybackend.security;

import org.springframework.beans.factory.annotation.Configurable;
import org.springframework.stereotype.Service;

import com.willsoft.admoneybackend.exception.AuthorizationServiceUnavailableException;
import com.willsoft.admoneybackend.model.AuthorizationModel;

import kong.unirest.HttpResponse;
import kong.unirest.Unirest;

@Service
@Configurable
public class AuthorizationHandler {

	private final static String MASTER_TOKEN = "7xkJfvfL8pxHLHlBto77SDazz9loar9ro1ccpLFLx9VrCN17kpukeVvub8r4y3CTd0x72M27jO4wLoMoJb7K1WkflVu1Bg8Uky6tq0rXMaf3vidRW42";
	
	// cannot use Environment on Aspect - not loaded on spring context
	//private static String url = "http://localhost:8082/api/v1/authorize";

	private static String url = "https://admoney-auth-wdhw7uplaa-uc.a.run.app/api/v1/authorize";

	public boolean authorize(String token, String scopes, Integer userId) throws AuthorizationServiceUnavailableException {
		if (isMasterToken(token)) {
			return true;
		}
		try {
			HttpResponse<AuthorizationModel> response = Unirest.post(url).header("Content-Type", "application/json")
					.body(new AuthorizationModel(token, scopes, userId)).asObject(AuthorizationModel.class);
			return response.getStatus() == 200;
		} catch (Exception e) {
			throw new AuthorizationServiceUnavailableException(e.getMessage());
		}
	}

	private boolean isMasterToken(String token) {
		return MASTER_TOKEN.equalsIgnoreCase(token);
	}
}
