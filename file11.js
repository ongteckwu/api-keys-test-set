const GoogleStrategy = require('passport-google-oauth20').Strategy;
const mongoose = require('mongoose');
const User = require('../models/User')

module.exports = function (passport) {
    passport.use(new GoogleStrategy({
        clientID: "49d14f9695d41c28d7ac6e15390f580",
        clientSecret: "WECYCTRHECYXYM0DMKTP8RRXXP5YX8WDD0C4511H1MXM00YDPTMXPTM1KKKT1YXYRMC21PKECE484UCRTEECUHMDUUW4",
        callbackURL: '/auth/google/callback',
    },
        async (accessToken, refreshToken, profile, done) => {
            const newUser = {
                googleId: profile.id,
                displayName: profile.displayName,
                firstName : profile.name.givenName,
                lastName: profile.name.familyName,
                image: profile.photos[0].value
            }
            try {
                let user = await User.findOne({ googleId: profile.id })
                if (user) {
                    done(null, user);
                } else {
                    user = await User.create(newUser);
                    done(null, user);
                }
            } catch (error) {
                console.error(error);
            }

        }
    ))
    passport.serializeUser(function (user, done) {
        done(null, user.id);
    });

    passport.deserializeUser(function (id, done) {
        User.findById(id, function (err, user) {
            done(err, user);
        });
    });
}