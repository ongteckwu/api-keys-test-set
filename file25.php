<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\User;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Mail;
use Illuminate\Support\Facades\Validator;
use Tymon\JWTAuth\Exceptions\UserNotDefinedException;

class UserController extends MasterController
{
    protected $model;
    protected $auth_key;

    public function __construct(User $model)
    {
        $this->model = $model;
        $this->auth_key = '9jXU0ONK_F3-dh1mkKGWp6zXPoZDNPW';
        parent::__construct();
    }
    public function validation_rules($method, $id = null)
    {
        if ($method == 2) {
            $rules['mobile'] = 'unique:users,mobile,' . $id;
            $rules['email'] = 'email|max:255|unique:users,email,' . $id;
        } else {
            $rules = [
                'mobile' => 'required|unique:users|max:13',
                'email' => 'required|unique:users|email|max:255',
                'name' => 'required',
                'password' => 'required',
                'device' => 'required',
            ];
        }
        return $rules;
    }
    function send_code($email,$activation_code){
        //Mail::to($email)->send(new ConfirmCode($activation_code));
    }

    function lang(){
        return request()->header('lang','ar');
    }

    public function validation_messages()
    {
        return array(
            'unique' => ' مسجل بالفعل :attribute هذا الـ',
            'required' => ':attribute يجب ادخال الـ',
        );
    }
    public function register(Request $request){
        $validator = Validator::make($request->all(),$this->validation_rules(1));
        if($this->lang()=='ar')
            $validator = Validator::make($request->all(),$this->validation_rules(1),$this->validation_messages());
        if ($validator->fails()) {
            return $this->sendError($validator->errors()->first());
        }
        $activation_code = rand(1111, 9999);
        $all = $request->all();
        $all['activation_code'] = $activation_code;
        $user = User::create($all);
        return $this->sendResponse($user);
    }
    public function login(Request $request){
        $cred=$request->only(['mobile','password']);
        $token=auth()->attempt($cred);
        return $this->sendResponse($token)->header();
    }

}
