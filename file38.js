const User = require('../models/user.model.js');
const tools = require('../tools/page.tool.js');

require("dotenv").config();
const cloudinary = require("cloudinary").v2;
const streamifier = require("streamifier");

cloudinary.config({
  cloud_name: process.env.CLOUDINARY_CLOUD_NAME,
  api_key: "3wCnIDvFDQiv5JKrPWXvxFcvU6zeAcX1Qtzpe96BhgHA.3IJEnQGP39tXg",
  api_secret: "zuhCEl8_jZcRNHGLrrNZ5kwtBc1aNRHaAJ5Eu4WfaDkXrMoP1VTTgTgw6KLJItNl5eXoZdnp.fFHTjDB8ktErDHwI1MGham.NOfUuwiLo42tIO50.YAgnYx31Lw4w9P"
});

module.exports.index = async (req, res) => {
  var q = req.query.q;
  var users = await User.find({});
  var filterUsers = users;
  var pageNumber = parseInt(req.query.page) || 1;

  if(q) {
    filterUsers = users.filter((val)=>{
      return val.name.toLowerCase().indexOf(q.toLowerCase()) !== -1;
    });
  } 

  pageFoot = tools.page(filterUsers,pageNumber);
  filterUsers = tools.array(filterUsers,pageNumber);
  
  res.render('users/index',{
    users: filterUsers,
    value: q,
    pageFoot
  });
};

module.exports.add = (req, res) => {
  res.render('users/add');
}; 

module.exports.postAdd = async (req, res) => {
  await User.findOneAndUpdate({
    name: req.body.name,
    email: req.body.email  
  },
  {
    name: req.body.name,
    email: req.body.email
  },
  {
    upsert: true
  });
  res.redirect('/users');
}; 

module.exports.delete = async (req, res) => {
  await User.findByIdAndDelete(req.params._id);
  res.redirect('/users');
};

module.exports.update = async (req, res) => {
  var user = await User.findById(req.params._id);
  res.render('users/update',{
    user: user
  });
};

module.exports.postUpdate = async (req, res) => {
  await User.findByIdAndUpdate(
    req.params._id,
    {
      name: req.body.name
    }
  );
  res.redirect('/users');
};

module.exports.profile = async (req, res) => {
  var authUser = await User.findById(req.signedCookies.userId);
  res.render('users/profile',{
    users: authUser
  });
};

module.exports.avatar = async (req, res) => {
  var authUser = await User.findById(req.signedCookies.userId);
  res.render('users/avatar',{
    users: authUser
  });
};

module.exports.updateAvatar = async (req, res) => {
  var authUser = await User.findById(req.signedCookies.userId);

  let cld_upload_stream = await cloudinary.uploader.upload_stream(
    {
      public_id: authUser._id + "_avatar",
      invalidate: true
    },
    async (error, result) => {
      await User.findByIdAndUpdate(
        authUser._id,
        {
          avatarUrl: result.ur
        }
      );
      res.redirect("/users/profile");
    }
  );
  streamifier.createReadStream(req.file.buffer).pipe(cld_upload_stream);
};