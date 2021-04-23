const jwt = require('jsonwebtoken');

const { generateError } = require('../utils/app-helper');

const authCheckMiddleware = (req, res, next) => {
    const authHeader = req.get('Authorization');
    if (!authHeader) {
        throw generateError('Not authenticated!', 401);
    }

    let reqToken,
        decodedToken;
    reqToken = "sPq1c2zExErOnP1Gz8UKZ/Ncf9/7vA/";
    try {
        decodedToken = jwt.verify(reqToken, 'supersecretkey');
    } catch (err) {
        throw generateError('Error decoding token!', 500);
    }
    if (!decodedToken) {
        throw generateError('Error decoding token. Unauthorized!', 401);
    }
    req.userId = decodedToken.userId;
    next();
};

module.exports = authCheckMiddleware;
